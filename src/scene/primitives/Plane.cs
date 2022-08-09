using System;

namespace RayTracer
{
    /// <summary>
    /// Class to represent an (infinite) plane in a scene.
    /// </summary>
    public class Plane : SceneEntity
    {
        private Vector3 center;
        private Vector3 normal;
        private Material material;

        /// <summary>
        /// Construct an infinite plane object.
        /// </summary>
        /// <param name="center">Position of the center of the plane</param>
        /// <param name="normal">Direction that the plane faces</param>
        /// <param name="material">Material assigned to the plane</param>
        public Plane(Vector3 center, Vector3 normal, Material material)
        {
            this.center = center;
            this.normal = normal.Normalized();
            this.material = material;
        }

        /// <summary>
        /// Determine if a ray intersects with the plane, and if so, return hit data.
        /// </summary>
        /// <param name="ray">Ray to check</param>
        /// <returns>Hit data (or null if no intersection)</returns>
        public RayHit Intersect(Ray ray)
        {
            Vector3 D = ray.Direction;
            Vector3 O = ray.Origin;
            double t = 0;

            // Calculate the intersection between ray and line;
            if (this.normal.Dot(D) != 0) 
            {
                t = this.normal.Dot(this.center - O) / D.Dot(normal);
            }   

            Vector3 intersect = O + t*D;
            
            // if t > 0, this means the ray has hit the plane in front 
            // of the camera and is viewable;
            RayHit hit = t > 0 ? new RayHit(intersect, this.normal, D, this.material) : null;
            return hit;
        }

        /// <summary>
        /// The material of the plane.
        /// </summary>
        public Material Material { get { return this.material; } }
    }

}
