using System.IO;
using System.Collections.Generic;
namespace RayTracer
{
    /// <summary>
    /// Add-on option C. You should implement your solution in this class template.
    /// </summary>
    public class ObjModel : SceneEntity
    {
        
        private double radiusSq;
        private const double BIAS = 1e-4;
        private Material material;
        private Triangle[] faces;

        private Vector3 center;
        /// <summary>
        /// Construct a new OBJ model.
        /// </summary>
        /// <param name="objFilePath">File path of .obj</param>
        /// <param name="offset">Vector each vertex should be offset by</param>
        /// <param name="scale">Uniform scale applied to each vertex</param>
        /// <param name="material">Material applied to the model</param>
        public ObjModel(string objFilePath, Vector3 offset, double scale, Material material)
        {
            this.material = material;
            List<Vector3> normals  = new List<Vector3>();
            List<Vector3> vertices = new List<Vector3>();
            List<Triangle> faces = new List<Triangle>();
            this.radiusSq = -1.0d;
            this.center = offset;

            // Here's some code to get you started reading the file...
            string[] lines = File.ReadAllLines(objFilePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] args = lines[i].Split();
                switch(args[0])
                {
                    case "v":
                        Vector3 vertex = new Vector3(double.Parse(args[1]), double.Parse(args[2]), double.Parse(args[3]));

                        if (vertex.LengthSq() > this.radiusSq) this.radiusSq = vertex.LengthSq();

                        vertices.Add((scale * vertex) + offset);
                        break;
                    case "vn":
                        Vector3 normal = new Vector3(double.Parse(args[1]), double.Parse(args[2]), double.Parse(args[3]));
                        normals.Add(normal);
                        break;
                    case "f":
                        string[] indices = args[1..^0];
                        Vector3[] triVerts = new Vector3[3];
                        Vector3[] triNorms = new Vector3[3];
                        for(int j=0; j<3; j++) 
                        {
                            int vertIndex = int.Parse(indices[j].Split("//")[0]);
                            int normIndex = int.Parse(indices[j].Split("//")[1]);
                            triVerts[j] = vertices[vertIndex - 1];
                            triNorms[j] = normals[normIndex - 1];
                        }
                        
                        faces.Add(new Triangle(triVerts[0], triVerts[1], triVerts[2], 
                                                    triNorms[0], triNorms[1], triNorms[2], 
                                                    this.material));
                        break;
                    default:
                        break;
                }
            }

            this.faces = faces.ToArray();
        }

        /// <summary>
        /// Given a ray, determine whether the ray hits the object
        /// and if so, return relevant hit data (otherwise null).
        /// </summary>
        /// <param name="ray">Ray data</param>
        /// <returns>Ray hit data, or null if no hit</returns>
        public RayHit Intersect(Ray ray)
        {
            
            // We simulate a bounding sphere around the object
            Vector3 Orig2Cent = this.center - ray.Origin;
            double triAdj = Orig2Cent.Dot(ray.Direction);
            double triHypSq = Orig2Cent.LengthSq();
            double triAdjSq = triAdj * triAdj;
            double triOppSq = triHypSq - triAdjSq;

            // We exit if the ray doesn't intersect the bounding sphere at all
            if (triOppSq > this.radiusSq) return null;

            // Otherwise we continue with calculating the closest triangle 
            // to the ray
            return ClosestHit(ray);
        }

        private RayHit ClosestHit(Ray ray)
        {
            double closest2origin= -1.0d;
            RayHit closest = null;

            foreach(var triangle in this.faces)
            {
                RayHit hit = triangle.Intersect(ray);
                if (hit != null && closest2origin == -1.0d) 
                {
                    closest = hit;
                    closest2origin = (hit.Position + BIAS*hit.Normal - ray.Origin).LengthSq();
                    continue;
                } 
                else if (hit != null && closest2origin != -1.0d)
                {
                    double hit2origin = (hit.Position + BIAS*hit.Normal - ray.Origin).LengthSq();
                    if (closest2origin > hit2origin)
                    {
                        closest = hit;
                        closest2origin = hit2origin;
                    }
                }
            }
            return closest;
        }

        /// <summary>
        /// The material attached to this object.
        /// </summary>
        public Material Material { get { return this.material; } }
    }

}
