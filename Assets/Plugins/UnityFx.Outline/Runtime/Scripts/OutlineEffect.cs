﻿// Copyright (C) 2019 Alexander Bogarsukov. All rights reserved.
// See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityFx.Outline
{
	/// <summary>
	/// Post-effect script. Should be attached to camera.
	/// </summary>
	/// <seealso cref="OutlineLayer"/>
	/// <seealso cref="https://willweissman.wordpress.com/tutorials/shaders/unity-shaderlab-object-outlines/"/>
	public sealed class OutlineEffect : MonoBehaviour
	{
		#region data

		private const string _effectName = "Outline";

#pragma warning disable 0649

		[SerializeField]
		private Shader _renderColorShader;
		[SerializeField]
		private Shader _postProcessShader;

#pragma warning restore 0649

		private List<OutlineLayer> _layers;
		private CommandBuffer _commandBuffer;
		private Material _renderMaterial;

		#endregion

		#region interface

		/// <summary>
		/// Gets or sets a <see cref="Shader"/> that renders objects outlined with a solid while color.
		/// </summary>
		public Shader RenderColorShader
		{
			get
			{
				return _renderColorShader;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("RenderColorShader");
				}

				if (_renderColorShader != value)
				{
					_renderColorShader = value;

					if (_renderMaterial)
					{
						_renderMaterial.shader = value;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets a <see cref="Shader"/> that renders outline around the mask, generated by <see cref="RenderColorShader"/>.
		/// </summary>
		public Shader PostProcessShader
		{
			get
			{
				return _postProcessShader;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("PostProcessShader");
				}

				if (_postProcessShader != value)
				{
					_postProcessShader = value;

					foreach (var layers in _layers)
					{
						layers.PostProcessMaterial.shader = _postProcessShader;
					}
				}
			}
		}

		/// <summary>
		/// Gets all registered outlie layers.
		/// </summary>
		public IEnumerable<OutlineLayer> Layers
		{
			get
			{
				if (_layers == null)
				{
					_layers = new List<OutlineLayer>();
				}

				return _layers;
			}
		}

		/// <summary>
		/// Creates a new outlie layer.
		/// </summary>
		public OutlineLayer AddLayer()
		{
			if (_layers == null)
			{
				_layers = new List<OutlineLayer>();
			}

			if (_renderMaterial == null)
			{
				_renderMaterial = new Material(_renderColorShader);
			}

			var layer = new OutlineLayer(_renderMaterial, new Material(_postProcessShader));
			_layers.Add(layer);
			return layer;
		}

		/// <summary>
		/// Removes the specified layer.
		/// </summary>
		public void RemoveLayer(OutlineLayer layer)
		{
			if (_layers != null)
			{
				_layers.Remove(layer);
			}
		}

		/// <summary>
		/// Removes all layers.
		/// </summary>
		public void RemoveAllLayers()
		{
			if (_layers != null)
			{
				_layers.Clear();
			}
		}

		#endregion

		#region MonoBehaviour

		private void OnEnable()
		{
			var camera = GetComponent<Camera>();

			if (camera)
			{
				_commandBuffer = new CommandBuffer();
				_commandBuffer.name = _effectName;

				FillCommandBuffer(_commandBuffer);

				camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, _commandBuffer);
			}
		}

		private void OnDisable()
		{
			var camera = GetComponent<Camera>();

			if (camera)
			{
				camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, _commandBuffer);
			}

			if (_commandBuffer != null)
			{
				_commandBuffer.Dispose();
				_commandBuffer = null;
			}
		}

		private void Update()
		{
			var needUpdate = false;

			foreach (var layer in _layers)
			{
				if (layer.IsChanged)
				{
					needUpdate = true;
					break;
				}
			}

			if (needUpdate)
			{
				FillCommandBuffer(_commandBuffer);
			}
		}

		#endregion

		#region implementation

		private void FillCommandBuffer(CommandBuffer cmdbuf)
		{
			var rtId = Shader.PropertyToID("_MainTex");
			var rt = new RenderTargetIdentifier(rtId);
			var dst = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);

			cmdbuf.BeginSample(_effectName);
			cmdbuf.Clear();
			cmdbuf.GetTemporaryRT(rtId, -1, -1, 0, FilterMode.Bilinear, RenderTextureFormat.R8);

			foreach (var layer in _layers)
			{
				layer.FillCommandBuffer(cmdbuf, rt, dst);
			}

			cmdbuf.ReleaseTemporaryRT(rtId);
			cmdbuf.EndSample(_effectName);
		}

		#endregion
	}
}
