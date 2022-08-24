using System;

namespace RayTracer
{
    /// <summary>
    /// Class to represent a triangle in a scene represented by three vertices.
    /// </summary>
    public class Triangle : SceneEntity
    {
        private Vector3 v0, v1, v2;
        private Vector3 normal;
        private Material material;

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
            this.normal = (v1 - v0).Cross(v2 - v0).Normalized();
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
            Vector3 triNormal = this.normal;
            
            if (triNormal.Dot(ray.Direction) < 0) 
            {
                // Literally copying from the slides lmao
                double t = triNormal.Dot(v0 - ray.Origin) / ray.Direction.Dot(triNormal);
                Vector3 point = ray.Origin + t * ray.Direction;

                // Finding if plane intersection point lies in the triangle
                // Someting about barycentric coordinates
                if ( triNormal.Dot((v1 - v0).Cross(point - v0)) >= 0 &&
                     triNormal.Dot((v2 - v1).Cross(point - v1)) >= 0 &&
                     triNormal.Dot((v0 - v2).Cross(point - v2)) >= 0 )
                {
                    RayHit hit = t > 0 ? new RayHit(point, triNormal, ray.Direction, this.material) : null;
                    return hit;
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
