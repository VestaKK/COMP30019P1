using System;
using System.Collections.Generic;

namespace RayTracer
{
    /// <summary>
    /// Class to represent a ray traced scene, including the objects,
    /// light sources, and associated rendering logic.
    /// </summary>
    public class Scene
    {
        private SceneOptions options;
        private ISet<SceneEntity> entities;
        private ISet<PointLight> lights;

        /// <summary>
        /// Construct a new scene with provided options.
        /// </summary>
        /// <param name="options">Options data</param>
        public Scene(SceneOptions options = new SceneOptions())
        {
            this.options = options;
            this.entities = new HashSet<SceneEntity>();
            this.lights = new HashSet<PointLight>();
        }

        /// <summary>
        /// Add an entity to the scene that should be rendered.
        /// </summary>
        /// <param name="entity">Entity object</param>
        public void AddEntity(SceneEntity entity)
        {
            this.entities.Add(entity);
        }

        /// <summary>
        /// Add a point light to the scene that should be computed.
        /// </summary>
        /// <param name="light">Light structure</param>
        public void AddPointLight(PointLight light)
        {
            this.lights.Add(light);
        }

        /// <summary>
        /// Render the scene to an output image. This is where the bulk
        /// of your ray tracing logic should go... though you may wish to
        /// break it down into multiple functions as it gets more complex!
        /// </summary>
        /// <param name="outputImage">Image to store render output</param>
        public void Render(Image outputImage)
        {
            Vector3 camera = this.options.CameraPosition;
            double gridSizeX = 1.0d/outputImage.Width;
            double gridSizeY = 1.0d/outputImage.Height;

            for (int i=0; i < outputImage.Width; i++)
            for (int j=0; j < outputImage.Height; j++)
            {   
                Ray ray = new Ray(camera, (ImagePlaneCoordinate((i + 0.5d) * gridSizeX, (j + 0.5d) * gridSizeY, 1.0d, outputImage) - camera).Normalized());

                foreach(var entity in this.entities)
                {
                    RayHit hit = entity.Intersect(ray);
                    
                    // Only shade in pixel if there is a hit detected;
                    // Condense into a function;
                    if (hit != null && SpaceIsClear(hit.Position, camera, entity))
                    {
                        Color pixelColor = CalculateColor(hit, entity);
                        outputImage.SetPixel(i, j, pixelColor);
                        break;
                    }
                }
            }
        }


        private Boolean SpaceIsClear(Vector3 origin, Vector3 destination, SceneEntity entity) 
        {
            Vector3 orig2Dest = (destination - origin).Normalized();
            Ray lineOfSight = new Ray(destination, -orig2Dest); 
            Boolean output = true;

            foreach(var otherEntity in this.entities)
            {
                // Only Works for planar and convex objects
                // TODO: Find a solution to thi later
                if (otherEntity.Equals(entity)) continue;

                RayHit anotherHit = otherEntity.Intersect(lineOfSight); 

                if (anotherHit != null) 
                {
                    Vector3 adjustedOrigin = origin + 0.0000000001 * orig2Dest;
                    Vector3 cmphit1 = adjustedOrigin - destination;
                    Vector3 cmphit2 = anotherHit.Position - destination;
                        
                    if (cmphit1.Dot(cmphit2) < cmphit1.LengthSq())
                    {
                        output = false;
                        break;
                    }
                }
            }

            return output;
        }

        private Color CalculateColor(RayHit hit, SceneEntity entity) 
        {
            Vector3 hitNormal = hit.Normal.Normalized();
            Vector3 adjustedHitPosition = hit.Position - 0.0000000001 * hit.Incident;
            Color pixelColor = new Color(0.0f, 0.0f, 0.0f);
            // Check if the hit Object is illuminated by any pointlights in the scene
            foreach (var pointLight in this.lights) 
            {
                // Check if the Soace between the entity and the pointlight is clear
                Boolean directLight = SpaceIsClear(adjustedHitPosition, pointLight.Position, entity);

                // We react accordingly based on the type of material that has been hit
                if (entity.Material.Type == Material.MaterialType.Diffuse) 
                {
                    // Diffuse Lighting
                    Vector3 hit2Light = (pointLight.Position - adjustedHitPosition).Normalized();

                    if (hitNormal.Dot(hit2Light) > 0 && directLight)
                    {
                        pixelColor += (entity.Material.Color * pointLight.Color) * hitNormal.Dot(hit2Light);
                    }
                }

                // Reflective
                if (entity.Material.Type == Material.MaterialType.Reflective || 
                    entity.Material.Type == Material.MaterialType.Refractive && 
                    directLight)
                {   
                    pixelColor = RecursiveReflections(hit, pixelColor, 0, entity);
                }   
            }
            return pixelColor;
        }

        private Color RecursiveReflections(RayHit currHit, Color pixelColor, int numReflections, SceneEntity currEntity) 
        {
            Vector3 reflectedVector = currHit.Incident - 2 * currHit.Incident.Dot(currHit.Normal.Normalized()) * currHit.Normal.Normalized(); 

            Vector3 adjustedHitPosition = currHit.Position + 0.0000000001 * reflectedVector.Normalized();
            Ray reflectedRay = new Ray(adjustedHitPosition, reflectedVector.Normalized());

            foreach (var nextEntity in this.entities) 
            {   

                RayHit nextHit = nextEntity.Intersect(reflectedRay);

                if (nextHit != null && numReflections < 10 && SpaceIsClear(nextHit.Position, adjustedHitPosition, currEntity))
                {
                    if (nextEntity.Material.Type == Material.MaterialType.Diffuse) 
                    {
                        pixelColor = CalculateColor(nextHit, nextEntity);
                        break;
                    }
                    else if (nextEntity.Material.Type == Material.MaterialType.Reflective || nextEntity.Material.Type == Material.MaterialType.Refractive)
                    {
                        pixelColor = RecursiveReflections(nextHit, pixelColor, numReflections + 1, nextEntity);
                    }
                }
            }
            return pixelColor;
        }

        private Vector3 ImagePlaneCoordinate(double x, double y, double z, Image outputImage)
        {
            // Defining plane as it appears when embedded in the scene
            double fieldOfView = 60.0d;
            double aspectRatio = outputImage.Width / outputImage.Height;
            double Deg2Rad = Math.PI/180.0d;

            // 1.0d not necessary, but it represents the distance from the camera
            double fovLength = 2.0d * Math.Tan(fieldOfView*Deg2Rad / 2) * 1.0d;
            double imagePlaneHeight = fovLength;
            double imagePlaneWidth = fovLength * aspectRatio;

            double cx = (x - 0.5d) * imagePlaneWidth;
            double cy = (0.5d - y) * imagePlaneHeight;
            double cz = z;

            return new Vector3(cx, cy, cz);
        }
    }
}
