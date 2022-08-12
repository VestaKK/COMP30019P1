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
        private readonly double BIAS = 1e-4;
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
                Ray ray = new Ray(camera, (ImagePlaneCoordinate((i + 0.5d) * gridSizeX, (j + 0.5d) * gridSizeY, outputImage) - camera).Normalized());

                foreach(var entity in this.entities)
                {
                    RayHit hit = entity.Intersect(ray);
                    
                    // Only shade in pixel if there is a hit detected;
                    // Condense into a function;
                    if (hit != null && LineOfSight(hit.Position, camera))
                    {
                        Color pixelColor = CalculateColor(hit, entity);
                        outputImage.SetPixel(i, j, pixelColor);
                        break;
                    }
                }
            }
        }

        private Boolean LineOfSight(Vector3 origin, Vector3 destination) 
        {
            Vector3 adjustedOrigin = origin + BIAS*(destination - origin);
            Vector3 orig2Dest = (destination - adjustedOrigin).Normalized();
            Ray lineOfSight = new Ray(destination, -orig2Dest); 
            Boolean output = true;

            foreach(var entity in this.entities)
            {
                RayHit hit = entity.Intersect(lineOfSight); 
                
                if (hit != null) 
                {
                    Vector3 cmphit1 = adjustedOrigin - destination;
                    Vector3 cmphit2 = hit.Position - destination;
                        
                    if (cmphit1.Dot(cmphit2) < cmphit1.LengthSq() && 
                        cmphit1.Dot(cmphit2) > 0)
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
            Color pixelColor = new Color(0.0f, 0.0f, 0.0f);

            // This is to prevent shadow acne
            RayHit alteredHit = new RayHit(hit.Position + (BIAS*hit.Normal), hit.Normal, hit.Incident, entity.Material);
            
            // Check if the hit Object is illuminated by any pointlights in the scene
            foreach (var pointLight in this.lights) 
            {
                // Check if the Soace between the entity and the pointlight is clear
                Boolean directLight = LineOfSight(alteredHit.Position, pointLight.Position);
                
                // We react accordingly based on the type of material that has been hit
                Vector3 hit2Light = (pointLight.Position - alteredHit.Position).Normalized();
                
                switch(entity.Material.Type)
                {
                    case Material.MaterialType.Diffuse:
                        if (hit.Normal.Dot(hit2Light) > 0 && directLight)
                            pixelColor += (entity.Material.Color * pointLight.Color) * hit.Normal.Dot(hit2Light);
                        break;

                    // not necessary to be in here for now but when the object's color comes into play this matters
                    case Material.MaterialType.Reflective:
                        pixelColor = RecursiveReflections(alteredHit, pixelColor, 0);
                        break;
                    // not necessary to be in here for now but when the object's color comes into play this matters
                    case Material.MaterialType.Refractive:
                        if (hit.Normal.Dot(hit2Light) > 0)
                            pixelColor = RecursiveRefraction(alteredHit, pixelColor, 0);
                        break;

                    default:
                        break;
                }
            }
            return pixelColor;
        }

        private Color RecursiveRefraction(RayHit currHit, Color pixelColor, int numReflections) 
        {
            return pixelColor;
        }

        private Color RecursiveReflections(RayHit currHit, Color pixelColor, int numReflections) 
        {
            if (numReflections > 10) return pixelColor;

            Vector3 reflectedVector = currHit.Incident - 2 * currHit.Incident.Dot(currHit.Normal.Normalized()) * currHit.Normal.Normalized(); 
            Ray reflectedRay = new Ray(currHit.Position, reflectedVector.Normalized());
            
            foreach (var nextEntity in this.entities) 
            {   
                RayHit nextHit = nextEntity.Intersect(reflectedRay);
                
                if (nextHit != null && LineOfSight(nextHit.Position, currHit.Position))
                {
                    switch(nextEntity.Material.Type)
                    {
                        case Material.MaterialType.Reflective:
                            pixelColor = RecursiveReflections(nextHit, pixelColor, numReflections + 1);
                            break;
                        default:
                            pixelColor = CalculateColor(nextHit, nextEntity);
                            break;
                    }
                }
            }
            return pixelColor;
        }

        private Vector3 ImagePlaneCoordinate(double x, double y, Image outputImage)
        {
            // Defining plane as it appears when embedded in the scene
            double fieldOfView = 60.0d;
            double aspectRatio = outputImage.Width / outputImage.Height;
            double Deg2Rad = Math.PI/180.0d;

            // 1.0d not necessary, but it represents the distance from the camera
            double fovLength = 2.0d * Math.Tan(fieldOfView*Deg2Rad / 2) * 1.0d;
            double imagePlaneHeight = fovLength;
            double imagePlaneWidth = fovLength * aspectRatio;

            // on the assumption that the image plane is centered on (0,0,1)
            double cx = (x - 0.5d) * imagePlaneWidth;
            double cy = (0.5d - y) * imagePlaneHeight;
            double cz = 1.0d;

            return new Vector3(cx, cy, cz);
        }
    }
}
