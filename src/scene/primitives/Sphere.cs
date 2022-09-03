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
        
        // Ray-Sphere Intersection with Simple Math
        // http://kylehalladay.com/blog/tutorial/math/2013/12/24/Ray-Sphere-Intersection.html
        // Altered to accomodate Refraction (internal ray-sphere intersections)
        public RayHit Intersect(Ray ray)
        {
            Vector3 Orig2Cent = this.center - ray.Origin;

            // We make a triangle between the origin, the center of the circle
            // and the shortest vector between the center of the circle and 
            // the ray, and solve for the triangle's side lengths.
            double triAdj = Orig2Cent.Dot(ray.Direction);

            // Using squared values where possible to increase speed of calculations
            double triHypSq = Orig2Cent.LengthSq();
            double triAdjSq = triAdj * triAdj;
            double triOppSq = triHypSq - triAdjSq;
            double radiusSq = this.radius*this.radius;

            // Apparently this check is enough to tell that the ray is hitting the 
            // Sphere, but we still have to compute the points of intersection
            if (triOppSq < radiusSq)
            {
                // Calculate distance from center of Sphere to the intersectioni points
                // We form a new triangle between the center, the intersection points
                // and the vector previously
                double innerTriAdj = Math.Sqrt(radiusSq - triOppSq);
                
                // We can now determine our intersection points
                double t1 = triAdj - innerTriAdj;
                double t2 = triAdj + innerTriAdj;
                Vector3 p1 = ray.Origin + t1 * ray.Direction;
                Vector3 p2 = ray.Origin + t2 * ray.Direction;
                Vector3 p1Normal = p1 - center;
                Vector3 p2Normal = p2 - center;

                // if the origin of the ray is outside the sphere, we know that our ray hits
                // p1. Otherwise we know that the ray has been fired from inside the sphere and we
                // have hit p2.
                return Orig2Cent.LengthSq() > radiusSq ? 
                       new RayHit(p1, p1Normal.Normalized(), ray.Direction.Normalized(), this.material) :
                       new RayHit(p2, p2Normal.Normalized(), ray.Direction.Normalized(), this.material);
            }
            
            return null;
        }

        /// <summary>
        /// The material of the sphere.
        /// </summary>
        public Material Material { get { return this.material; } }
    }

}
