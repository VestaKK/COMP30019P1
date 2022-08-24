using System.IO;
using System.Collections.Generic;
namespace RayTracer
{
    /// <summary>
    /// Add-on option C. You should implement your solution in this class template.
    /// </summary>
    public class ObjModel : SceneEntity
    {
        private const double BIAS = 1e-4;
        private Material material;
        List<Vector3> vertices;
        List<Vector3> normals;
        List<Triangle> faces;
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
            this.normals  = new List<Vector3>();
            this.vertices = new List<Vector3>();
            this.faces = new List<Triangle>();
            // Here's some code to get you started reading the file...
            string[] lines = File.ReadAllLines(objFilePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] args = lines[i].Split();
                switch(args[0])
                {
                    case "v":
                        Vector3 vertex = new Vector3(double.Parse(args[1]), double.Parse(args[2]), double.Parse(args[3]));
                        vertices.Add(scale * (vertex + offset));
                        break;
                    case "vn":
                        Vector3 normal = new Vector3(double.Parse(args[1]), double.Parse(args[2]), double.Parse(args[3]));
                        vertices.Add(normal);
                        break;
                    case "f":

                        string[] indices = args[1..^0];
                        Vector3[] triVerts = new Vector3[3];

                        for(int j=0; j<3; j++) 
                        {
                            int vertIndex = int.Parse(indices[j].Split("//")[0]);
                            int normIndex = int.Parse(indices[j].Split("//")[1]);
                            triVerts[j] = this.vertices[j];
                        }

                        this.faces.Add(new Triangle(triVerts[0], 
                                                    triVerts[1], 
                                                    triVerts[2], 
                                                    this.material));
                        break;
                }
            }
        }

        /// <summary>
        /// Given a ray, determine whether the ray hits the object
        /// and if so, return relevant hit data (otherwise null).
        /// </summary>
        /// <param name="ray">Ray data</param>
        /// <returns>Ray hit data, or null if no hit</returns>
        public RayHit Intersect(Ray ray)
        {
            RayHit closest = null;
            foreach(Triangle face in this.faces) 
            {
                RayHit faceHit = face.Intersect(ray);

                if (faceHit != null || !(closest != null)) 
                {
                    closest = faceHit;
                    continue;
                }
                else
                {
                    Vector3 cmphit1 = faceHit.Position + BIAS*faceHit.Normal - ray.Origin;
                    Vector3 cmphit2 = closest.Position + BIAS*closest.Normal - ray.Origin;
                    if (cmphit2.Dot(cmphit1) < cmphit1.LengthSq())
                    {
                        closest = faceHit;
                    }
                }
            }
            // Write your code here...
            return closest;
        }

        /// <summary>
        /// The material attached to this object.
        /// </summary>
        public Material Material { get { return this.material; } }
    }

}
