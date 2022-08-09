using System;

namespace RayTracer
{
    /// <summary>
    /// Class to represent an (infinite) plane in a scene.
    /// </summary>
    public class Sphere : SceneEntity
    {
        private Vector3 center;
        private double radius;
        private Material material;

        /// <summary>
        /// Construct a sphere given its center point and a radius.
        /// </summary>
        /// <param name="center">Center of the sphere</param>
        /// <param name="radius">Radius of the spher</param>
        /// <param name="material">Material assigned to the sphere</param>
        public Sphere(Vector3 center, double radius, Material material)
        {
            this.center = center;
            this.radius = radius;
            this.material = material;
        }

        /// <summary>
        /// Determine if a ray intersects with the sphere, and if so, return hit data.
        /// </summary>
        /// <param name="ray">Ray to check</param>
        /// <returns>Hit data (or null if no intersection)</returns>
        public RayHit Intersect(Ray ray)
        {
            Vector3 Orig2Cent = this.center - ray.Origin;

            // We make a triangle between the origin, the center of the circle
            // and a point on the ray perpendicular to the ray that also
            // points to the center 
            double triAdj = Orig2Cent.Dot(ray.Direction.Normalized());
            double triOpp = Math.Sqrt(Orig2Cent.LengthSq() - triAdj * triAdj);

            // Apparently this check is enough to tell that the ray is hitting the 
            // Sphere, but we still have to compute the points of intersection
            if (triOpp > this.radius)
            {
                return null;
            } 
            else
            {
                RayHit hit = new RayHit(this.center, this.center, this.center, this.material);
                return hit;
            }
        }

        /// <summary>
        /// The material of the sphere.
        /// </summary>
        public Material Material { get { return this.material; } }
    }

}
