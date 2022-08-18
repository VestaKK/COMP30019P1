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
                        Color pixelColor = CalculateColor(hit);
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
                        return false;
                    }
                }
            }
            return true;
        }
        
        private Color CalculateColor(RayHit hit) 
        {
            Color pixelColor = new Color(0.0f, 0.0f, 0.0f);
            switch (hit.Material.Type) 
            {
                case Material.MaterialType.Diffuse:
                    pixelColor = DiffuseLighting(hit, pixelColor, hit.Material);
                    break;
                case Material.MaterialType.Reflective:
                    pixelColor = RecursiveReflection(hit, pixelColor, 0);
                    break;
                case Material.MaterialType.Refractive:
                    pixelColor = RecursiveRefraction(hit, pixelColor, 0);
                    break;
                default:
                    break;
            }
            return pixelColor;
        }

        private Color DiffuseLighting(RayHit hit, Color pixelColor, Material material) 
        {
            // This is to prevent shadow acne
            RayHit altHit = new RayHit(hit.Position + (BIAS*hit.Normal), hit.Normal, hit.Incident, hit.Material);
            
            foreach (var pointLight in this.lights) 
            {
                // Check if the Soace between the entity and the pointlight is clear
                Boolean directLight = LineOfSight(altHit.Position, pointLight.Position);
                // We react accordingly based on the type of material that has been hit
                Vector3 hit2Light = (pointLight.Position - altHit.Position).Normalized();
                if (altHit.Normal.Dot(hit2Light) > 0 && directLight)
                    pixelColor += (material.Color * pointLight.Color) * altHit.Normal.Dot(hit2Light);
            }
            return pixelColor;
        }

        private Color RecursiveRefraction(RayHit currHit, Color pixelColor, int numRefractions) 
        {   
            if (numRefractions > 5) return pixelColor;
            RayHit altHit;
            Vector3 I = currHit.Incident;
            Vector3 N = currHit.Normal;
            double etaT = currHit.Material.RefractiveIndex;
            double etaI = 1.0d;

            if (N.Dot(I) < 0) 
            {
                altHit = new RayHit(currHit.Position - BIAS*currHit.Normal, currHit.Normal, currHit.Incident, currHit.Material);
            }
            else
            {
                altHit = new RayHit(currHit.Position + BIAS*currHit.Normal, currHit.Normal, currHit.Incident, currHit.Material);
                N = -1*N;
                double tmp = etaT;
                etaT = etaI;
                etaI = tmp;
            }

            double eta = etaI/etaT;
            double c1 = N.Dot(I);
            double c2 = Math.Sqrt(1 - (eta*eta)*(1 - N.Dot(I) * N.Dot(I)));
            Vector3 T = (eta*I + (eta*c1 - c2)*N).Normalized();

            System.Console.WriteLine("Yeet");
            System.Console.WriteLine(I.ToString());
            System.Console.WriteLine(T.ToString());

            Ray transmit = new Ray(altHit.Position, T);
            foreach(var entity in this.entities) {
                RayHit nextHit = entity.Intersect(transmit);
                if (nextHit != null && LineOfSight(nextHit.Position, transmit.Origin))
                {
                    Vector3 center = new Vector3(0.25, -0.2, 1.5);
                    System.Console.WriteLine(nextHit.Material.Type.ToString());
                    System.Console.WriteLine((altHit.Position - center).Length());
                    System.Console.WriteLine((nextHit.Position - center).Length());
                    
                }
            }
            
            return pixelColor;
        }

        private Color RecursiveReflection(RayHit currHit, Color pixelColor, int numReflections) 
        {
            if (numReflections > 15) return pixelColor;
            RayHit altHit = new RayHit(currHit.Position + (BIAS*currHit.Normal), currHit.Normal, currHit.Incident, currHit.Material);

            Vector3 reflectedVector = altHit.Incident - 2 * altHit.Incident.Dot(altHit.Normal) * altHit.Normal; 
            Ray reflectedRay = new Ray(altHit.Position, reflectedVector.Normalized());
            
            foreach (var nextEntity in this.entities) 
            {   
                RayHit nextHit = nextEntity.Intersect(reflectedRay);
                
                if (nextHit != null && LineOfSight(nextHit.Position, altHit.Position))
                {
                    switch(nextEntity.Material.Type)
                    {
                        case Material.MaterialType.Reflective:
                            pixelColor = RecursiveReflection(nextHit, pixelColor, numReflections + 1);
                            break;
                        default:
                            pixelColor = CalculateColor(nextHit);
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
