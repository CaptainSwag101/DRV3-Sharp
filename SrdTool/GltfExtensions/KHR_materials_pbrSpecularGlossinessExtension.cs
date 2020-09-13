using System;
using System.Collections.Generic;
using glTFLoader.Schema;

namespace SrdTool.GltfExtensions
{
    class KHR_materials_pbrSpecularGlossinessExtension : Extension
    {
        public float[] diffuseFactor { get; set; }
        public TextureInfo diffuseTexture { get; set; }
        public float[] specularFactor { get; set; }
        public float glossinessFactor { get; set; }
        public TextureInfo specularGlossinessTexture { get; set; }
        public Dictionary<string, object> extensions { get; set; }
        public Extras[] extras { get; set; }

        public KHR_materials_pbrSpecularGlossinessExtension()
        {
            diffuseFactor = new float[] { 1.0f, 1.0f, 1.0f, 1.0f };
            specularFactor = new float[] { 1.0f, 1.0f, 1.0f };
            glossinessFactor = 1.0f;
        }
            
    }
}
