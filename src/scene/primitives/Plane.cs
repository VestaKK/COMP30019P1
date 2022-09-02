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

        private Boolean isRefractive;

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
            this.isRefractive = material.Type == Material.MaterialType.Refractive ? true : false;
        }

        /// <summary>
        /// Determine if a ray intersects with the plane, and if so, return hit data.
        /// </summary>
        /// <param name="ray">Ray to check</param>
        /// <returns>Hit data (or null if no intersection)</returns>
        public RayHit Intersect(Ray ray)
        {
            Vector3 direction = ray.Direction;
            Vector3 origin = ray.Origin;
            
            // if the normal and the ray are facing the same direction
            // the place is hit from behind
            if (this.normal.Dot(direction) < 0 || isRefractive) 
            {
                // Calculate the distance to the plane
                double t = this.normal.Dot(this.center - origin) / direction.Dot(normal);
                Vector3 intersect = origin + t*direction;

                // if t > 0, this means the ray has hit the plane in front 
                // of the camera and is viewable;
                return t > 0 || isRefractive ? new RayHit(intersect, this.normal.Normalized(), direction.Normalized(), this.material) : null;
            }   
            
            return null;
        }

        /// <summary>
        /// The material of the plane.
        /// </summary>
        public Material Material { get { return this.material; } }
    }

}
