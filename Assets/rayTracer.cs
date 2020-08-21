using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;

public class rayTracer : MonoBehaviour {

    public GameObject emitter;

    public GameObject scannedObject;

    public Light testLight;

    public float maxRange = 100f;


    public int maxBounces = 3;


    int[] triangles;
    MeshFilter mf;
    Vector3[] vertices;

    Vector3[] hitPoints;
    float[] amountHits;

    float[] amountHits_raw;

    float[] lightEnergy;

    Color[] initColors;

    bool isCasting = false;

    bool isCasting_single = false;

    bool isCasting_cone = false;

    bool isCasting_reflect = false;

    int hitCounter = 0;





	// Use this for initialization
	void Start () {

        mf = scannedObject.GetComponent<MeshFilter>();

        triangles = mf.mesh.triangles;
        vertices = mf.mesh.vertices;
        





        hitPoints = new Vector3[vertices.Length];
        amountHits = new float[vertices.Length];

        lightEnergy = new float[vertices.Length];

        initColors = new Color[vertices.Length];

        for (int i = 0; i < hitPoints.Length; i++)
        {
            hitPoints[i] = Vector3.zero;

            amountHits[i] = 0;

            lightEnergy[i] = 0;

            initColors[i] = new Color(1f, 1f, 1f);
        }

	}
	
	

    bool rayCasterFun(Vector3 position, Vector3 direction, out RaycastHit outputHit,int currBounces, bool showRay = true)
    {
        RaycastHit hit;

        if (Physics.Raycast(position, direction, out hit, maxRange))
        {

            float lightPower = 0f;
            if (currBounces < 0)
            {
                currBounces = 1;
            }
            else
            {
                lightPower =( (float)currBounces / (float)maxBounces ) * Mathf.Cos(Vector3.Angle(direction,hit.normal));
            }
            
            
            if (hit.transform.tag == "scanObj")
            {


                for (int m = 0; m < 3; m++)
                {
                    var test = triangles[hit.triangleIndex * 3 + m];

                    hitPoints[test] = vertices[test];
                    lightEnergy[test] += lightPower;
                    amountHits[test] += 1;


                }
                hitCounter++;

                if (showRay)
                {
                    Debug.DrawRay(position, direction * hit.distance, Color.green, 60);
                }
                
            }
            else
            {
                if (showRay)
                {
                    Debug.DrawRay(position, direction * hit.distance, Color.blue, 60);
                }

            }

            outputHit = hit;
 
            return true;
        }
        else
        {

            if (showRay)
            {
                Debug.DrawRay(position, direction * 10, Color.red, 60);
            }
           
            outputHit = hit;
            return false;
        }

    }

    void trackRaycastPath_multiple(GameObject cone, Vector3 conePos, Vector3 coneDir,Quaternion orientation, int numberOfBouncesLeft)
    {


        float angleDelta = 0.05f;
        float radius = 1f;
        float radiusDelta = 0.1f;

        float distanceOfCone = 5;

        float angleOfSpread = 80;
        int numDir = 15;

        

        List<Vector3> lightConeDirections = new List<Vector3>();

        lightConeDirections = calculateConeDirections(conePos, coneDir, orientation, angleDelta, radius, radiusDelta, distanceOfCone);




        //lightConeDirections.Count
        for (int i = 0; i < lightConeDirections.Count; i++)
        {
            
            RaycastHit currRaycastHit;
            bool isSuccess = true;
            isSuccess = rayCasterFun(conePos, lightConeDirections[i], out currRaycastHit, maxBounces + 1);

            Vector3 tangentN;
            Vector3 bidirect;
            Matrix4x4 newMat;

            //float currGlossiness = currRaycastHit.transform.gameObject.GetComponent<amountOfGlossiness>().glossiness;
            float currGlossiness = 0;
            if (currGlossiness == 0)
            {
                tangentN = new Vector3(currRaycastHit.normal.z, 0, -currRaycastHit.normal.x) / Mathf.Sqrt(currRaycastHit.normal.x * currRaycastHit.normal.x + currRaycastHit.normal.z * currRaycastHit.normal.z);
                bidirect = Vector3.Cross(currRaycastHit.normal, tangentN);
                newMat = new Matrix4x4(new Vector4(tangentN.x, tangentN.y, tangentN.z, 0),
                        new Vector4(bidirect.x, bidirect.y, bidirect.z, 0), new Vector4(currRaycastHit.normal.x, currRaycastHit.normal.y, currRaycastHit.normal.z, 0),
                        new Vector4(0, 0, 0, 1));
            }
            else
            {
                Vector3 glossyReflect = Vector3.Reflect(lightConeDirections[i], currRaycastHit.normal);

                tangentN = new Vector3(glossyReflect.z, 0, -glossyReflect.x) / Mathf.Sqrt(glossyReflect.x * glossyReflect.x + glossyReflect.z * glossyReflect.z);
                bidirect = Vector3.Cross(glossyReflect, tangentN);
                newMat = new Matrix4x4(new Vector4(tangentN.x, tangentN.y, tangentN.z, 0),
                        new Vector4(bidirect.x, bidirect.y, bidirect.z, 0), new Vector4(glossyReflect.x, glossyReflect.y, glossyReflect.z, 0),
                        new Vector4(0, 0, 0, 1));
            }






            if (isSuccess)
            {

                Vector3[] lightConeDirections_arr = new Vector3[numDir];


                if (currGlossiness == 0)
                {
                    lightConeDirections_arr = randomHemisphereDirsV2(numDir);
                }
                else
                {
                    lightConeDirections_arr = randomHemisphereDirs_cosPower(numDir, currGlossiness);





                }
                




                recursiveRayCaster(currRaycastHit, lightConeDirections_arr, newMat, maxBounces, numDir);



            }



        }


    }


    void recursiveRayCaster(RaycastHit currHitStart, Vector3[] castingDirs, Matrix4x4 newMat, int numberOfBounces, int numDirections)
    {

        if (numberOfBounces < 0)
        {
            return;
        }




        numberOfBounces -= 1;

        for (int i = 0; i < castingDirs.Length; i++)
        {



            RaycastHit currRaycastHit_ref;
            bool isSuccess2 = true;

            Vector3 currDirVec = newMat.MultiplyVector(castingDirs[i]);



            isSuccess2 = rayCasterFun(currHitStart.point, currDirVec, out currRaycastHit_ref, numberOfBounces, false);

            float orientationalTest = Vector3.Dot(currDirVec, currHitStart.normal);



            if (!isSuccess2 || orientationalTest < 0)
            {
                
                continue;
            }

            

            Vector3 tangentN;
            Vector3 bidirect;
            Matrix4x4 newMat_rec;

            
            //float currGlossiness = currRaycastHit_ref.transform.gameObject.GetComponent<amountOfGlossiness>().glossiness;
            float currGlossiness = 0;
            if (currGlossiness == 0)
            {
                tangentN = new Vector3(currRaycastHit_ref.normal.z, 0, -currRaycastHit_ref.normal.x) / Mathf.Sqrt(currRaycastHit_ref.normal.x * currRaycastHit_ref.normal.x + currRaycastHit_ref.normal.z * currRaycastHit_ref.normal.z);
                bidirect = Vector3.Cross(currRaycastHit_ref.normal, tangentN);
                newMat_rec = new Matrix4x4(new Vector4(tangentN.x, tangentN.y, tangentN.z, 0),
                        new Vector4(bidirect.x, bidirect.y, bidirect.z, 0), new Vector4(currRaycastHit_ref.normal.x, currRaycastHit_ref.normal.y, currRaycastHit_ref.normal.z, 0),
                        new Vector4(0, 0, 0, 1));
            }
            else
            {
                Vector3 glossyReflect = Vector3.Reflect(currDirVec, currRaycastHit_ref.normal);

                tangentN = new Vector3(glossyReflect.z, 0, -glossyReflect.x) / Mathf.Sqrt(glossyReflect.x * glossyReflect.x + glossyReflect.z * glossyReflect.z);
                bidirect = Vector3.Cross(glossyReflect, tangentN);
                newMat_rec = new Matrix4x4(new Vector4(tangentN.x, tangentN.y, tangentN.z, 0),
                        new Vector4(bidirect.x, bidirect.y, bidirect.z, 0), new Vector4(glossyReflect.x, glossyReflect.y, glossyReflect.z, 0),
                        new Vector4(0, 0, 0, 1));
            }




            if (isSuccess2)
            {
                Vector3[] reflConeDirections = new Vector3[numDirections];


                //reflConeDirections = randomHemisphereDirs((currRaycastHit_ref.normal - castingDirs[i]), 80, numDirections);

                //reflConeDirections = randomHemisphereDirsV2(numDirections);

                if (currGlossiness == 0)
                {
                    reflConeDirections = randomHemisphereDirsV2(numDirections);
                }
                else
                {
                    reflConeDirections = randomHemisphereDirs_cosPower(numDirections, currGlossiness);
                }


                //reflConeDirections = randomHemisphereDirs_cosPower(3, 50);


                //reflConeDirections[0] = Vector3.Reflect(currDirVec, currRaycastHit_ref.normal);








                //RaycastHit currRaycastHit_ref2;
                //rayCasterFun(currRaycastHit_ref.point, reflConeDirections[0], out currRaycastHit_ref2);

                recursiveRayCaster(currRaycastHit_ref, reflConeDirections, newMat_rec, numberOfBounces, numDirections);

                
            }






        }



    }



    void trackRaycastPath(Vector3 startPos, Vector3 startDir, int numberOfBounces)
    {
        bool isSuccess = true;

        Vector3 currPos = startPos;

        Vector3 currDir = startDir;

        for (int i = 0; i < numberOfBounces; i++)
        {


            RaycastHit currRaycastHit;
            isSuccess = rayCasterFun(currPos, currDir, out currRaycastHit, numberOfBounces);

            if (isSuccess)
            {

                currPos = currRaycastHit.point;
                currDir = Vector3.Reflect(currDir, currRaycastHit.normal);
            }
            else
            {
               // Debug.Log("End");
                break;


            }

        }
    }


    List<Vector3> calculateConeDirections(Vector3 coneStart, Vector3 coneOrientation, Quaternion coneRotation,  float angleDelta = 0.01f, float radius = 1f, float radiusDelta = 0.1f, float distanceOfCone = 5)
    {
        float x1 = coneStart.x;
        float y1 = coneStart.y;
        float z1 = coneStart.z;

        List<Vector3> outputDirList = new List<Vector3>();

        for (float i = 0; i < 1; i = i + angleDelta)
        {
            for (float j = 0.1f; j < radius; j = j + radiusDelta)
            {
                float a = 2f * Mathf.PI * i;



                float x = j * Mathf.Sin(a) ;
                float y = j * Mathf.Cos(a) ;


                //Vector3 tempPos = new Vector3(x, y, z1);

                //Vector3 endPos = coneOrientation * distanceOfCone + tempPos;

                Vector3 endPos = coneStart + coneOrientation * distanceOfCone + coneRotation * new Vector3(x, y);

                Vector3 newDir = new Vector3(x1 - endPos.x, y1 - endPos.y, z1 - endPos.z).normalized;

                outputDirList.Add(-newDir);

            }



        }

        return outputDirList;

    }


    Vector3 RandomSpotLightCirclePoint(Light spot)
    {
        float radius = Mathf.Tan(Mathf.Deg2Rad * spot.spotAngle / 2) * spot.range;
        Vector2 circle = Random.insideUnitCircle * radius;
        Vector3 target = spot.transform.position + spot.transform.forward * spot.range + spot.transform.rotation * new Vector3(circle.x, circle.y);
        return target;
    }


    List<Vector3> calculateConeDirections_proper(GameObject emitterObj, float angleDelta = 0.01f, float radius = 1f, float radiusDelta = 0.1f, float distanceOfCone = 5)
    {



        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);




        List<Vector3> dirsCone = new List<Vector3>();

        plane.transform.position = emitterObj.transform.forward * distanceOfCone + emitterObj.transform.position;

        plane.transform.rotation = emitterObj.transform.rotation;

        plane.GetComponent<MeshCollider>().enabled = false;

        GameObject container = new GameObject();

        container.transform.position = plane.transform.position;

        container.name = "container";


        for (float i = 0; i < 1; i = i + angleDelta)
        {
            for (float j = 0.1f; j < radius; j = j + radiusDelta)
            {

                float y = Mathf.Cos(2f * Mathf.PI * i) * j + plane.transform.localPosition.y;
                float x = Mathf.Sin(2f * Mathf.PI * i) * j + plane.transform.localPosition.x;


                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);



                sphere.transform.localPosition = new Vector3(x, y, plane.transform.localPosition.z);


                sphere.GetComponent<SphereCollider>().enabled = false;

                sphere.name = "sphere" + i;

                sphere.transform.parent = container.transform;

            }
        }

        container.transform.rotation = plane.transform.rotation;



        float x1 = emitterObj.transform.localPosition.x;
        float y1 = emitterObj.transform.localPosition.y;
        float z1 = emitterObj.transform.localPosition.z;

        foreach (Transform child in container.transform)
        {


            Vector3 newDir = new Vector3(x1 - child.transform.position.x, y1 - child.transform.position.y, z1 - child.transform.position.z).normalized;

            dirsCone.Add(-newDir);

            //Debug.DrawRay(emitter.transform.position, -newDir * 10, Color.red, 60);

        }

        Destroy(container);
        Destroy(plane);





        return dirsCone;

    }


    public static Vector3 GetPointOnUnitSphereCap(Quaternion targetDirection, float angle)
    {
        float angleInRad = Random.Range(0.0f, angle) * Mathf.Deg2Rad;
        Vector2 PointOnCircle = (Random.insideUnitCircle.normalized) * Mathf.Sin(angleInRad);
        Vector3 V = new Vector3(PointOnCircle.x, PointOnCircle.y, Mathf.Cos(angleInRad));
        return targetDirection * V;
    }
    public static Vector3 GetPointOnUnitSphereCap(Vector3 targetDirection, float angle)
    {
        return GetPointOnUnitSphereCap(Quaternion.LookRotation(targetDirection), angle);
    }


    Vector3[] randomHemisphereDirs(Vector3 dirOfHemisphere, float angleOfSpread, int numDirs)
    {

        float angle = angleOfSpread;

        Vector3[] outputRandomDirs = new Vector3[numDirs];

        for (int i = 0; i < numDirs; i++)
        {
            //Vector3 target = RandomSpotLightCirclePoint(testLight);
            //Debug.DrawLine(target, target + Vector3.one * 0.01f, Color.red, 0.5f);



            Quaternion rot = Quaternion.LookRotation(dirOfHemisphere);

            Vector3 V = GetPointOnUnitSphereCap(rot, angle);

            outputRandomDirs[i] = V;

            //Debug.DrawRay(emitter.transform.position, V, Color.red, 60);
        }

        return outputRandomDirs;
    }

    Vector3[] randomHemisphereDirsV2(int numDirs)
    {

        

        Vector3[] outputRandomDirs = new Vector3[numDirs];

        for (int i = 0; i < numDirs; i++)
        {

            //float u1 = Random.value;
            //float u2 = Random.value;
            //float r = Mathf.Sqrt(u1);
            //float theta = 2 * Mathf.PI * u2;

            //float x = r * Mathf.Cos(theta);
            //float y = r * Mathf.Sin(theta);

            //Vector3 testVec = new Vector3(x, y, Mathf.Sqrt(Mathf.Max(0.0f, 1 - u1)));


            float r1 = Random.Range(0.0f, 1.0f);
            float r2 = Random.Range(0.0f, 1.0f);

            float sinTheta = Mathf.Sqrt(1 - r1 * r1);
            float phi = 2 * Mathf.PI * r2;
            float x = sinTheta * Mathf.Cos(phi);
            float z = sinTheta * Mathf.Sin(phi);

            Vector3 testVec = new Vector3(x, z, r1).normalized;

            
            

            outputRandomDirs[i] = testVec;

           
        }

        return outputRandomDirs;
    }


    Vector3[] randomHemisphereDirs_cosPower(int numDirs, float cosPow = 0f)
    {
        Vector3[] outputRandomDirs = new Vector3[numDirs];

        for (int i = 0; i < numDirs; i++)
        {

            float u = Random.Range(0.0f, 1.0f);
            float v = Random.Range(0.0f, 1.0f);

            

            float theta = Mathf.Acos(Mathf.Pow((1 - u), (1 / (1 + cosPow))));

            float phi = 2 * Mathf.PI * v;

            float x = Mathf.Sin(theta) * Mathf.Cos(phi);
            float y = Mathf.Sin(theta) * Mathf.Sin(phi);

            Vector3 testVec = new Vector3(x, y, Mathf.Cos(theta)).normalized;


            outputRandomDirs[i] = testVec;
        }

        return outputRandomDirs;
    }



	void Update () {


        if (Input.GetKeyDown(KeyCode.Z))
        {
            isCasting_cone = !isCasting_cone;
        }



        if (isCasting_cone)
        {
           

            float angleDelta = 0.01f;
            float radius = 1f;
            float radiusDelta = 0.1f;

            float distanceOfCone = 2;

            List<Vector3> lightConeDirections = new List<Vector3>();

            lightConeDirections = calculateConeDirections_proper(emitter, angleDelta, radius, radiusDelta, distanceOfCone);


            for (int i = 0; i < lightConeDirections.Count; i++)
            {
                trackRaycastPath(emitter.transform.position, lightConeDirections[i], 10);
               // Debug.DrawRay(emitter.transform.position, lightConeDirections[i] * 10, Color.red);
            }

            

        }



        if (Input.GetKeyDown(KeyCode.A))
        {
            

            float startTime = Time.realtimeSinceStartup;

            

            trackRaycastPath_multiple(emitter,emitter.transform.position, emitter.transform.forward, emitter.transform.rotation, 2);

            Debug.Log("Time it took: " + (Time.realtimeSinceStartup - startTime));




            for (int i = 0; i < amountHits.Length; i++)
            {
                if (amountHits[i] != 0)
                {
                    initColors[i] = new Color(amountHits[i], 0f, 0f);
                }
            }
            

            mf.mesh.colors = initColors;

        }





        if (Input.GetKeyDown(KeyCode.S))
        {
            StreamWriter streamWriter = new StreamWriter("C:\\Users\\ivan\\Documents\\RayPathTracing\\Assets\\Results\\Obj_brightness.txt");
            string output = "";

            for (int i = 0; i < vertices.Length; i++)
            {
                output = vertices[i].x.ToString() + " " + vertices[i].y.ToString() + " " + vertices[i].z.ToString() + " " + lightEnergy[i].ToString() + " " + amountHits[i].ToString();
                
                streamWriter.WriteLine(output);

            }

            streamWriter.Close();

            Debug.Log("Saved Object Brightness");
        }

		
	}
}
