using UnityEngine;

namespace Physics {
    public enum MaterialType {
        Metal,
        Wood,
        Plastic,
        Rubber
    }

    public class Material {
        public string name;
        public MaterialType type;
        public float density;      // الكثافة
        public float elasticity;   // معامل المرونة
        public float friction;     // معامل الاحتكاك
        public float hardness;     // الصلابة
        public float deformability; // قابلية التشوه (جديد)
        public Color color;        // لون المادة

    
        public static Material Metal {
            get {
                return new Material {
                    name = "Metal",
                    type = MaterialType.Metal,
                    density = 7.8f,
                    elasticity = 0.3f,
                    friction = 0.2f,
                    hardness = 0.8f,
                    deformability = 0.1f,
                    color = new Color(0.7f, 0.7f, 0.7f)
                };
            }
        }

        public static Material Wood {
            get {
                return new Material {
                    name = "Wood",
                    type = MaterialType.Wood,
                    density = 0.7f,
                    elasticity = 0.5f,
                    friction = 0.4f,
                    hardness = 0.3f,
                    deformability = 0.5f,
                    color = new Color(0.6f, 0.4f, 0.2f)
                };
            }
        }

        public static Material Plastic {
            get {
                return new Material {
                    name = "Plastic",
                    type = MaterialType.Plastic,
                    density = 0.9f,
                    elasticity = 0.7f,
                    friction = 0.3f,
                    hardness = 0.4f,
                    deformability = 0.6f,
                    color = new Color(0.2f, 0.2f, 0.2f)
                };
            }
        }

        public static Material Rubber {
            get {
                return new Material {
                    name = "Rubber",
                    type = MaterialType.Rubber,
                    density = 0.9f,
                    elasticity = 0.9f,
                    friction = 0.8f,
                    hardness = 0.2f,
                    deformability = 0.8f,
                    color = new Color(0.1f, 0.1f, 0.1f)
                };
            }
        }
    }
} 