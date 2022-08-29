using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace RayTracer
{
    /// <summary>
    /// Add-on option C. You should implement your solution in this class template.
    /// </summary>
    public class ObjModel : SceneEntity
    {
        
        private Vector3 bMin;
        private Vector3 bMax;

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
            this.center = offset;
            

            List<Vector3> normals  = new List<Vector3>();
            List<Vector3> vertices = new List<Vector3>();
            List<Triangle> faces = new List<Triangle>();

            // To get the bounding box
            double minX = double.PositiveInfinity;
            double maxX = double.NegativeInfinity;
            double minY = double.PositiveInfinity;
            double maxY = double.NegativeInfinity;
            double minZ = double.PositiveInfinity;
            double maxZ = double.NegativeInfinity;

            // Here's some code to get you started reading the file...
            string[] lines = File.ReadAllLines(objFilePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] args = lines[i].Split();
                switch(args[0])
                {
                    case "v":
                        Vector3 vertex = new Vector3(double.Parse(args[1]), double.Parse(args[2]), double.Parse(args[3]));

                        if (vertex.X < minX) minX = vertex.X;
                        if (vertex.X > maxX) maxX = vertex.X;
                        if (vertex.Y < minY) minY = vertex.Y;
                        if (vertex.Y > maxY) maxY = vertex.Y;
                        if (vertex.Z < minZ) minZ = vertex.Z;
                        if (vertex.Z > maxZ) maxZ = vertex.Z;

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
            this.bMin = scale * (new Vector3(minX, minY, minZ)) + offset;
            this.bMax = scale * (new Vector3(maxX, maxY, maxZ)) + offset;
        }

        /// <summary>
        /// Given a ray, determine whether the ray hits the object
        /// and if so, return relevant hit data (otherwise null).
        /// </summary>
        /// <param name="ray">Ray data</param>
        /// <returns>Ray hit data, or null if no hit</returns>
        public RayHit Intersect(Ray ray)
        {
            return BoundingBoxHit(ray) ? ClosestTriangle(ray): null;
        }

        private bool BoundingBoxHit(Ray ray) {

            double tx0;
            double tx1;
            double ty0;
            double ty1;
            double tz0;
            double tz1;
            
            double inverseDX = 1 / ray.Direction.X;
            if (ray.Direction.X >= 0) 
            {
                tx0 = (this.bMin.X - ray.Origin.X) * inverseDX;
                tx1 = (this.bMax.X - ray.Origin.X) * inverseDX;
            }
            else
            {
                tx0 = (this.bMax.X - ray.Origin.X) * inverseDX;
                tx1 = (this.bMin.X - ray.Origin.X) * inverseDX;
            }

            double inverseDY = 1 / ray.Direction.Y;
            if (ray.Direction.Y >= 0) 
            {
                ty0 = (this.bMin.Y - ray.Origin.Y) * inverseDY;
                ty1 = (this.bMax.Y - ray.Origin.Y) * inverseDY;
            }
            else
            {
                ty0 = (this.bMax.Y - ray.Origin.Y) * inverseDY;
                ty1 = (this.bMin.Y - ray.Origin.Y) * inverseDY;
            }

            if (tx0 > ty1 || tx1 < ty0) return false;

            if (tx0 < ty0) tx0 = ty0;
            if (tx1 > ty1) tx1 = ty1;

            double inverseDZ = 1 / ray.Direction.Z;
            if (ray.Direction.Z >= 0) 
            {
                tz0 = (this.bMin.Z - ray.Origin.Z) * inverseDZ;
                tz1 = (this.bMax.Z - ray.Origin.Z) * inverseDZ;
            }
            else
            {
                tz0 = (this.bMax.Z - ray.Origin.Z) * inverseDZ;
                tz1 = (this.bMin.Z - ray.Origin.Z) * inverseDZ;
            }
            
            if (tx0 > tz1 || tx1 < tz0) return false;

            return true;
        }

        private RayHit ClosestTriangle(Ray ray)
        {
            double closestDist = double.PositiveInfinity;
            Vector3 closestVec = new Vector3(0.0f, 0.0f, 0.0f);
            RayHit closest = null;

            Parallel.ForEach(this.faces, triangle => {
                
                RayHit hit = triangle.Intersect(ray);

                if (hit == null) return;

                RayHit altHit = new RayHit(hit.Position - BIAS*hit.Incident, hit.Normal, hit.Incident, hit.Material);
                Vector3 currentVec = altHit.Position - ray.Origin;

                if (closestDist > currentVec.LengthSq() &&
                    currentVec.Dot(ray.Direction) > 0)
                {
                    closest = hit;
                    closestVec = currentVec;
                    closestDist = (currentVec).LengthSq();
                }
            });

            return closest;
        }

        /// <summary>
        /// The material attached to this object.
        /// </summary>
        public Material Material { get { return this.material; } }
    }

}
