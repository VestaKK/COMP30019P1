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
        private const double BIAS = 1e-4;
        private const double maxDepth = 100;
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
            double AAMultiplier = this.options.AAMultiplier;
            
            if (AAMultiplier == 1.0d) 
            {
                for (int i=0; i < outputImage.Width; i++)
                for (int j=0; j < outputImage.Height; j++)
                {   
                    Ray ray = new Ray(camera, (ImagePlaneCoordinate((i + 0.5d) * gridSizeX, 
                                                                    (j + 0.5d) * gridSizeY, outputImage) - camera).Normalized());
                    RayHit closest = ClosestHit(ray);

                    Color pixelColor = closest == null ?  new Color(0.0f, 0.0f, 0.0f) : CalculateColor(closest, 0);
                    outputImage.SetPixel(i, j, pixelColor);
                }
            }
            else 
            {
                // calculation for the dimensions of a subpixel
                double pixelPartition = 1.0d/AAMultiplier;

                for (int i=0; i < outputImage.Width; i++)
                for (int j=0; j < outputImage.Height; j++) 
                {
                    Color outputColor = new Color(0.0f, 0.0f, 0.0f);

                    for (int px=0; px < this.options.AAMultiplier; px++)
                    for (int py=0; py < this.options.AAMultiplier; py++)
                    {
                        
                        // Fire a ray through each subpixel of a given pixel
                        Ray ray = new Ray(camera, (ImagePlaneCoordinate((i + (px + 0.5) * pixelPartition) * gridSizeX, 
                                                                        (j + (py + 0.5) * pixelPartition) * gridSizeY, outputImage) - camera).Normalized());

                        // Find which surface the ray hits first
                        RayHit closest = ClosestHit(ray);

                        // Add surface colour to the output Color
                        outputColor += closest == null ?  new Color(0.0f, 0.0f, 0.0f) : CalculateColor(closest, 0);
                    }

                    // Average the colour values between the subpixels scanned
                    outputImage.SetPixel(i, j, outputColor/(AAMultiplier * AAMultiplier));
                }
            }
        }

        // Used for firing of Primary Rays and Secondary Rays
        private RayHit ClosestHit(Ray ray)
        {
            double closestDist = double.PositiveInfinity;
            Vector3 closestVec = new Vector3(0.0f, 0.0f, 0.0f);
            RayHit closest = null;

            foreach(var entity in this.entities)
            {
                RayHit hit = entity.Intersect(ray);

                if (hit == null) continue;

                RayHit altHit = new RayHit(hit.Position - BIAS*hit.Incident, hit.Normal, hit.Incident, hit.Material);
                Vector3 currentVec = altHit.Position - ray.Origin;

                if (closestDist > currentVec.LengthSq() &&
                    currentVec.Dot(ray.Direction) > 0)
                {
                    closest = hit;
                    closestVec = currentVec;
                    closestDist = (currentVec).LengthSq();
                }
            }
            return closest;
        }
        
        // Only used for diffuse Lighting because its more efficient there
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
        
        private Color CalculateColor(RayHit hit, int depth) 
        {
            switch (hit.Material.Type) 
            {
                case Material.MaterialType.Diffuse:
                    return DiffuseLighting(hit);
                case Material.MaterialType.Reflective:
                    return RecursiveReflection(hit, depth);
                case Material.MaterialType.Refractive:
                    return RecursiveRefraction(hit, depth);
                default:
                    return new Color(0.0f, 0.0f, 0.0f);
            }
        }

        private Color DiffuseLighting(RayHit hit) 
        {
            // This is to prevent shadow acne
            Color surfaceColor = new Color(0.0f, 0.0f, 0.0f);
            RayHit altHit = new RayHit(hit.Position + (BIAS*hit.Normal), hit.Normal, hit.Incident, hit.Material);
            
            foreach (var pointLight in this.lights) 
            {
                // Check if the Soace between the entity and the pointlight is clear
                Boolean directLight = LineOfSight(altHit.Position, pointLight.Position);
                // We react accordingly based on the type of material that has been hit
                Vector3 hit2Light = (pointLight.Position - altHit.Position).Normalized();
                if (altHit.Normal.Dot(hit2Light) > 0 && directLight)
                    surfaceColor += (hit.Material.Color * pointLight.Color) * altHit.Normal.Dot(hit2Light);
            }
            return surfaceColor;
        }

        private double Fresnel(double etaI, double etaT, double cosI) 
        {
            double eta = etaI/etaT;
            double sinI = Math.Sqrt(1 - cosI * cosI);
            double sinT = eta * sinI;
            double cosT = Math.Sqrt(1 - sinT * sinT);

            if (sinT >= 1) return 1.0d;

            double FRPll = ((etaI * cosI) - (etaT * cosT))/((etaI * cosI) + (etaT * cosT));
            double FRPpd = ((etaT * cosI) - (etaI * cosT))/((etaT * cosI) + (etaI * cosT));

            double FR = ((FRPll * FRPll) + (FRPpd * FRPpd))/2;
            return FR;
        }

        private Color RecursiveRefraction(RayHit currHit, int depth) 
        {   
            
            if (depth > maxDepth) return new Color(0.0f, 0.0f, 0.0f);

            Color refractedColor = new Color(0.0f, 0.0f, 0.0f);
            Color reflectedColor = new Color(0.0f, 0.0f, 0.0f);
            RayHit altHit;
            Vector3 I;
            Vector3 N;

            // etaT is the index refraction of the tramission material
            // etaI is the index of refraction of incident material
            // eta is the ratio between these two variables
            double etaT;
            double etaI;
            double eta;

            // if this is true it implies that we are hitting the object from the outside
            // We adjust the hit point below the surfave of the object to prevent self intersection
            if (currHit.Normal.Dot(currHit.Incident) < 0) 
            {
                altHit = new RayHit(currHit.Position - BIAS*currHit.Normal, currHit.Normal, currHit.Incident, currHit.Material);
                I = currHit.Incident;
                N = currHit.Normal;
                etaT = currHit.Material.RefractiveIndex;
                etaI = 1.0d;
            }

            // Otherwise we are hitting the object from inside the object
            // We adjust the hit point above the surface to prevent self intersection
            // Normal has to be flipped for the calculations to work out
            else
            {
                altHit = new RayHit(currHit.Position + BIAS*currHit.Normal, currHit.Normal, currHit.Incident, currHit.Material);
                I = currHit.Incident;
                N = currHit.Normal.Reversed();
                etaT = 1.0d;
                etaI = currHit.Material.RefractiveIndex;
            }

            eta = etaI/etaT;
            double cosI = N.Dot(I.Reversed());           
            double k = 1 - eta * eta * (1 - cosI * cosI);

            // if k < 0, our refracted angle is larger than the critical angle
            // Ray is fully reflected. This should only happen when the reflection is internal
            if (k < 0) 
            {
                RayHit internalHit = new RayHit(currHit.Position, N, currHit.Incident, currHit.Material);
                return RecursiveReflection(internalHit, depth + 1);
            }
            
            // We now create the refracted ray, knowing that refraction will occur
            Vector3 T = ((eta*cosI - Math.Sqrt(k))*N + eta*I).Normalized();
            Ray transmitted = new Ray(altHit.Position, T);

            RayHit nextHit = ClosestHit(transmitted);

            if (nextHit != null)
            {
                switch(nextHit.Material.Type)
                {
                    case Material.MaterialType.Refractive:
                        refractedColor = RecursiveRefraction(nextHit, depth + 1);
                        break;
                    default:
                        refractedColor = CalculateColor(nextHit, depth + 1);
                        break;
                }
            }
            
            double FR = Fresnel(etaI, etaT, cosI);
            reflectedColor = RecursiveReflection(currHit, depth + 1);

            return (1 - FR)*refractedColor + FR*reflectedColor;
        }

        private Color RecursiveReflection(RayHit currHit, int depth) 
        {
            if (depth > maxDepth) return new Color(0.0f, 0.0f, 0.0f);

            Color surfaceColor = new Color(0.0f, 0.0f, 0.0f);
            RayHit altHit = new RayHit(currHit.Position + (BIAS*currHit.Normal), currHit.Normal, currHit.Incident, currHit.Material);

            Vector3 reflectedVector = altHit.Incident - 2 * altHit.Incident.Dot(altHit.Normal) * altHit.Normal; 
            Ray reflectedRay = new Ray(altHit.Position, reflectedVector.Normalized());
            
            RayHit nextHit = ClosestHit(reflectedRay);
                
            if (nextHit != null)
            {
                switch(nextHit.Material.Type)
                {
                    case Material.MaterialType.Reflective:
                        surfaceColor = RecursiveReflection(nextHit, depth + 1);
                        break;
                    default:
                        surfaceColor = CalculateColor(nextHit, depth + 1);
                        break;
                }
            }

            return surfaceColor;
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
            
            // Using Rodrigues' Rotation Formula
            Vector3 _axisX = new Vector3(1.0f, 0.0f, 0.0f);
            Vector3 _axisY = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 _axisZ = new Vector3(0.0f, 0.0f, 1.0f);

            Vector3 axisR = this.options.CameraAxis.Normalized();

            double cameraAngle = this.options.CameraAngle % 360;

            if (cameraAngle > 180)
            {
                cameraAngle -= 360;
            } 
            else if (cameraAngle < -180) 
            {
                cameraAngle += 360;
            }

            double cosT = Math.Cos(Deg2Rad * (this.options.CameraAngle));
            double sinT = Math.Sqrt(1 - cosT * cosT);

            if (this.options.CameraAngle < 0) sinT *= -1;

            // adjusted for left hand coordinate system
            Vector3 axisX = _axisX*cosT + (axisR.Cross(_axisX))*sinT + axisR*(axisR.Dot(_axisX))*(1 - cosT);
            Vector3 axisY = _axisY*cosT + (axisR.Cross(_axisY))*sinT + axisR*(axisR.Dot(_axisY))*(1 - cosT);
            Vector3 axisZ = _axisZ*cosT + (axisR.Cross(_axisZ))*sinT + axisR*(axisR.Dot(_axisZ))*(1 - cosT);

            return (x - 0.5d)*imagePlaneWidth*axisX + 
                   (0.5d - y)*imagePlaneHeight*axisY + 
                   axisZ + 
                   this.options.CameraPosition;
        }
    }
}
