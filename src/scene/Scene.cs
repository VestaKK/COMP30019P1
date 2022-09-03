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
        private const double BIAS = 1e-5;
        private const double maxDepth = 20;
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

            // Dimensions of a pixel in world space
            double gridSizeX = 1.0d/outputImage.Width;
            double gridSizeY = 1.0d/outputImage.Height;

            // Our loops change depending if there is anti-aliasing
            if (this.options.AAMultiplier == 1.0d) 
            {
                for (int i=0; i < outputImage.Width; i++)
                for (int j=0; j < outputImage.Height; j++)
                {   
                    // Fire a ray through each subpixel of a given pixel from the camera
                    Ray ray = new Ray(camera, (ImagePlaneCoordinate((i + 0.5d) * gridSizeX, 
                                                                    (j + 0.5d) * gridSizeY, outputImage) - camera).Normalized());

                    // Find which surface the ray hits first
                    RayHit closest = ClosestHit(ray);

                    // Output the colour;
                    Color pixelColor = closest == null ?  new Color(0.0f, 0.0f, 0.0f) : CalculateColor(closest, 0);
                    outputImage.SetPixel(i, j, pixelColor);
                }
            }
            else 
            {
                // Calculation for the dimensions of a subpixel
                double pixelPartition = 1.0d/this.options.AAMultiplier;

                for (int i=0; i < outputImage.Width; i++)
                for (int j=0; j < outputImage.Height; j++) 
                {
                    Color outputColor = new Color(0.0f, 0.0f, 0.0f);

                    for (int px=0; px < this.options.AAMultiplier; px++)
                    for (int py=0; py < this.options.AAMultiplier; py++)
                    {
                        
                        // Fire a ray through each subpixel of a given pixel from the camera
                        Ray ray = new Ray(camera, (ImagePlaneCoordinate((i + (px + 0.5) * pixelPartition) * gridSizeX, 
                                                                        (j + (py + 0.5) * pixelPartition) * gridSizeY, outputImage) - camera).Normalized());

                        // Find which surface the ray hits first
                        RayHit closest = ClosestHit(ray);

                        // Add surface colour to the output Color
                        outputColor += closest != null ? CalculateColor(closest, 0) : new Color(0.0f, 0.0f, 0.0f);
                    }

                    // Average the colour values between the subpixels scanned and shade the pixel
                    outputImage.SetPixel(i, j, outputColor/(this.options.AAMultiplier * this.options.AAMultiplier));
                }
            }
        }

        /// <summary>
        /// Returns a RayHit for the closest surface that is hit by the ray 
        /// </summary>
        private RayHit ClosestHit(Ray ray)
        {
            // Store information about the closest viewable surface
            double closestDist = double.PositiveInfinity;
            RayHit closest = null;

            foreach(var entity in this.entities)
            {
                // See if the ray hits an entity
                RayHit hit = entity.Intersect(ray);

                if (hit == null) continue;

                // We adjust the hit point of the RayHit to avoid errors due to floating point precision issues
                RayHit altHit = new RayHit(hit.Position - BIAS*hit.Incident, hit.Normal, hit.Incident, hit.Material);
                Vector3 currentVec = altHit.Position - ray.Origin;

                // Compare hits to determine the closest surface to the ray
                if (closestDist > currentVec.LengthSq() &&
                    currentVec.Dot(ray.Direction) > 0)
                {
                    closest = hit;
                    closestDist = (currentVec).LengthSq();
                }
            }
            return closest;
        }
        
        /// <summary>
        /// Checks if there is clear space between two given coordinates
        /// </summary>
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
        
        /// <summary>
        /// Generic function that returns a colour based on the hit
        /// surface material
        /// </summary>
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
        
        /// <summary>
        /// Returns a color based on Diffuse lighting
        /// </summary>
        private Color DiffuseLighting(RayHit hit) 
        {
            // Output color
            Color surfaceColor = new Color(0.0f, 0.0f, 0.0f);

            // This is to prevent shadow acne
            RayHit altHit = new RayHit(hit.Position + (BIAS*hit.Normal), hit.Normal, hit.Incident, hit.Material);
            
            foreach (var pointLight in this.lights) 
            {
                // Check if the Soace between the entity and the pointlight is clear
                Boolean directLight = LineOfSight(altHit.Position, pointLight.Position);

                // Determine whether or not the surface is facing towards the pointLight
                Vector3 hit2Light = (pointLight.Position - altHit.Position).Normalized();
                if (altHit.Normal.Dot(hit2Light) > 0 && directLight)
                    surfaceColor += (hit.Material.Color * pointLight.Color) * altHit.Normal.Dot(hit2Light);
            }
            
            return surfaceColor;
        }

        /// <summary>
        /// Calculates Fresnel coefficient
        /// </summary>

        // Honestly, scratchapixel did more harm than good, took me 2 days to realise
        // the equations were wrong
        // https://www.scratchapixel.com/lessons/3d-basic-rendering/introduction-to-shading/reflection-refraction-fresnel#:~:text=Remember%20that%20ray-tracing%20is,reflection%20direction%20in%20this%20example
        // https://en.wikipedia.org/wiki/Fresnel_equations
        private double Fresnel(double etaI, double etaT, double cosI) 
        {
            double eta = etaI/etaT;
            double sinI = Math.Sqrt(1 - cosI * cosI);
            double sinT = eta * sinI;
            double cosT = Math.Sqrt(1 - sinT * sinT);

            // just in case the earlier check for total internal
            // reflection failed
            if (sinT >= 1) return 1.0d;

            double FRPll = ((etaI * cosI) - (etaT * cosT))/((etaI * cosI) + (etaT * cosT));
            double FRPpd = ((etaT * cosI) - (etaI * cosT))/((etaT * cosI) + (etaI * cosT));

            double FR = ((FRPll * FRPll) + (FRPpd * FRPpd))/2;
            return FR;
        }


        /// <summary>
        /// Recursively calculates the color of a given refractive material
        /// </summary>
        private Color RecursiveRefraction(RayHit currHit, int depth) 
        {   
            // Restricts number of recursions to make computer not explode :>
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
            // We adjust the hit point below the surface of the object to prevent self intersection
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
            
            // Some variable names used from Scratchapixel, as well as the equations for
            // the refracted beam and total internal reflection
            // https://www.scratchapixel.com/lessons/3d-basic-rendering/introduction-to-shading/reflection-refraction-fresnel#:~:text=Remember%20that%20ray-tracing%20is,reflection%20direction%20in%20this%20example
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
            
            // Create the refracted ray
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

        /// <summary>
        /// Recursively calculates the color of a given reflective material
        /// </summary>
        private Color RecursiveReflection(RayHit currHit, int depth) 
        {
            // Base condition for recursive loop
            if (depth > maxDepth) return new Color(0.0f, 0.0f, 0.0f);

            Color surfaceColor = new Color(0.0f, 0.0f, 0.0f);

            // Avoiding surface acne by pushing the hit point about the surface of the material
            RayHit altHit = new RayHit(currHit.Position + (BIAS*currHit.Normal), currHit.Normal, currHit.Incident, currHit.Material);

            // Equation taken from here
            // https://math.stackexchange.com/questions/13261/how-to-get-a-reflection-vector
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
            double aspectRatio = outputImage.Width / (double) outputImage.Height;
            double Deg2Rad = Math.PI/180.0d;

            // 1.0d not necessary, but it represents the distance from the camera
            double fovLength = 2.0d * Math.Tan(fieldOfView * Deg2Rad / 2) * 1.0d;
            double imagePlaneHeight = fovLength;
            double imagePlaneWidth = imagePlaneHeight * aspectRatio;
            
            // Define basis vectors in the camera space
            Vector3 _axisX = new Vector3(1.0f, 0.0f, 0.0f);
            Vector3 _axisY = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 _axisZ = new Vector3(0.0f, 0.0f, 1.0f);

            // Normalising makes user input easier
            Vector3 axisR = this.options.CameraAxis.Normalized();

            // Because the equations only consider angles ranging from
            // 0 -> 180 degress, we want to expand this to a full
            // -360 -> 360 by converting angles over 180 degrees to their
            // equivalent negative angle i.e. 190 == -170 degrees
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

            // The value of sinT changes based on sin(-T) = -sin(T)
            // Allows for anti-clockwise rotation i.e. -180 -> 180 degree rotation
            if (cameraAngle < 0) sinT *= -1;

            // Use Rodrigues' Rotation Formula to calculate axis' in world space
            Vector3 axisX = _axisX*cosT + (axisR.Cross(_axisX))*sinT + axisR*(axisR.Dot(_axisX))*(1 - cosT);
            Vector3 axisY = _axisY*cosT + (axisR.Cross(_axisY))*sinT + axisR*(axisR.Dot(_axisY))*(1 - cosT);
            Vector3 axisZ = _axisZ*cosT + (axisR.Cross(_axisZ))*sinT + axisR*(axisR.Dot(_axisZ))*(1 - cosT);

            // Calculate the pixel coordinate, on top of shifting them according to the camera's position
            return (x - 0.5d)*imagePlaneWidth*axisX + 
                   (0.5d - y)*imagePlaneHeight*axisY + 
                   axisZ + 
                   this.options.CameraPosition;
        }
    }
}
