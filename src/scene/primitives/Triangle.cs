using System;

namespace RayTracer
{
    /// <summary>
    /// Class to represent a triangle in a scene represented by three vertices.
    /// </summary>
    public class Triangle : SceneEntity
    {
        private const double BIAS = 1e-4;
        private Vector3 v0, v1, v2;
        private Vector3 n0, n1, n2;
        private Vector3 faceNormal;
        private Material material;

        private Boolean hasVertexNormals;

        /// <summary>
        /// Construct a triangle object given three vertices.
        /// </summary>
        /// <param name="v0">First vertex position</param>
        /// <param name="v1">Second vertex position</param>
        /// <param name="v2">Third vertex position</param>
        /// <param name="material">Material assigned to the triangle</param>
        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2, Material material)
        {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;

            this.faceNormal = (v1 - v0).Cross(v2 - v0);

            this.n0 = this.faceNormal;
            this.n1 = this.faceNormal;
            this.n2 = this.faceNormal;

            this.hasVertexNormals = false;
            this.material = material;
        }

        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2, 
                        Vector3 n0, Vector3 n1, Vector3 n2, 
                        Material material)
        {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;

            this.n0 = n0;
            this.n1 = n1;
            this.n2 = n2;

            this.faceNormal = (v1 - v0).Cross(v2 - v0);

            this.hasVertexNormals = true;
            this.material = material;
        }

        /// <summary>
        /// Determine if a ray intersects with the triangle, and if so, return hit data.
        /// </summary>
        /// <param name="ray">Ray to check</param>
        /// <returns>Hit data (or null if no intersection)</returns>
        public RayHit Intersect(Ray ray)
        {
            Vector3 v0 = this.v0;
            Vector3 v1 = this.v1;
            Vector3 v2 = this.v2;
            Vector3 faceNormal = this.faceNormal;
            
            if (faceNormal.Dot(ray.Direction) < 0) 
            {
                // Literally copying from the slides lmao
                // Look I don't make the rules fair game is fair game
                double t = faceNormal.Dot(v0 - ray.Origin) / ray.Direction.Dot(faceNormal);
                
                // if t < 0, the triangle is being hit from behind
                if (t < 0) return null;

                Vector3 point = ray.Origin + t * ray.Direction;

                // Finding if plane intersection point lies in the triangle
                // Using inside Out test
                if ( faceNormal.Dot((v1 - v0).Cross(point - v0)) >= 0 && 
                     faceNormal.Dot((v2 - v1).Cross(point - v1)) >= 0 && 
                     faceNormal.Dot((v0 - v2).Cross(point - v2)) >= 0 )
                {
                    if(!hasVertexNormals) 
                    {
                        return new RayHit(point, faceNormal.Normalized(), ray.Direction, this.material); 
                    }
                    else 
                    {
                        // Calculate Barycentric Coordinates
                        double area = faceNormal.Length();
                        double v = ((v1 - v0).Cross(point - v0).Length()) / area;
                        double u = ((v2 - v1).Cross(point - v1).Length()) / area;

                        Vector3 interpolatedNormal = v * n0 + u * n1 + (1 - u - v) * n2;

                        return new RayHit(point, interpolatedNormal.Normalized(), ray.Direction, this.material);   
                    }
                } 
            }
            return null;
        }
        /// <summary>
        /// The material of the triangle.
        /// </summary>
        public Material Material { get { return this.material; } }
    }
}
