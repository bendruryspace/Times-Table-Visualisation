using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UI;

public class TimeTable : MonoBehaviour
{
    public enum ColourState { global,independent, directionModLocal,directionModGlobal}

    public enum CaptureType { NoCapture, EditorScreenshot, RenderSingleShot, RenderRangeFactor, RenderRangePoints }
    
    [Header("Screenshot")]
    public CaptureType captureType;
    public int resWidth = 2550;
    public int resHeight = 3300;
    public float captureRangeStart;
    public float captureRangeEnd;
    public int framerate = 24;
    public float renderTime;    

    [Header("Table")]
    public int numberOfPoints = 4;
    public float factor;
    float Factor
    {
        get { return Mathf.Repeat(factor, numberOfPoints); }
        set { factor = Mathf.Repeat(value, numberOfPoints); }
    }
    int FactorFloor { get { return (Mathf.FloorToInt(Factor)); } }
    public float radius = 1;
    public Vector3 offset = Vector3.zero;
    
    [Header("visualise data")]
    public bool showLines = true;
    public bool showOutlines = true;
    public bool showFactorPosition = true;
    public bool showText;

    [Header("Points")]
    public float pointSize = .01f;
    [Range(0, 1)]
    public float persentageOfCircle = 1;    

    [Header("Lines")]
    public float lineWidth = 1;
    public ColourState lineColour = ColourState.global;
    public Color globalLineColour = Color.red;

    [Header("AutoRun")]
    public bool runAutomaticaly = false;
    public float secondsForRotation;
    public bool loop = false;
    public float countdownToStart = 3;
    public int RangeStart;
    public int RangeEnd;    

    float timeCounter = 0;    
    
    [Header("Mesh Positions")]
    public Vector3 lineStart;
    public Vector3 lineEnd;

    [Header("External Refs")]
    public string folder;
    public Text text;
    public Camera shotCamera;
    public Mesh pointMesh;
    public Material mat;


    private int counter;

    MeshFilter meshFilter;
    Mesh mesh;

    MaterialPropertyBlock scrollingPointMaterialBlock;
    MaterialPropertyBlock[] pointMaterialBlock;    
    Mesh[] meshPointLines;

    Matrix4x4 staticSimpleMatrixPoint;

    // 
    Vector3[] points;    
    float Increment { get { return (360 * persentageOfCircle) / numberOfPoints; } }
    float halfPI { get { return Mathf.PI / 180; } }    

    private void OnValidate()
    {
        UpdatePointPositions();
    }

    private void Start()
    {
        // Standard Setup
        UpdatePointPositions();

        SetupStaticMatrix();
        SetupNumberOfPoints();
        SetupScrollingPoint();

        if (captureType == CaptureType.RenderRangeFactor)
        {
            Iterations = Mathf.CeilToInt(framerate * renderTime);
            print(Iterations);
            iterator = 0;
        }
        else if (captureType == CaptureType.RenderRangePoints)
        {
            Iterations = Mathf.CeilToInt(framerate * renderTime);
            if (captureRangeEnd-captureRangeStart<Iterations)
            {
                Iterations = Mathf.RoundToInt(captureRangeEnd - captureRangeStart);
            }

            print(Iterations);
            iterator = 0;
        }
    }

    int Iterations;
    int iterator;
    private void Update()
    {
        switch (captureType)
        {
            case CaptureType.NoCapture:
                AutoUpdateFactor();
                PaintingToGraphics();
                break;

            case CaptureType.RenderSingleShot:
                // take shot
                PaintingToGraphics();
                string singleShot = uniqueFilename();
                if (showText)
                {
                    text.text = string.Format("Benjamin Drury Times Table - Points {0,0:D3} Factor {1,0:000.000}", numberOfPoints, Factor); 
                }
                ScreenCapture.CaptureScreenshot(singleShot);
                print(singleShot);
                EditorApplication.isPlaying = false;
                break;
            case CaptureType.RenderRangeFactor:
                if (iterator == 0)
                {
                    Factor = captureRangeStart;
                    PaintingToGraphics();
                    if (showText)
                    {
                        text.text = string.Format("Benjamin Drury Times Table - Points {0,0:D3} Factor {1,0:000.000} Range {2,0:000.000} - {3,0:000.000}", numberOfPoints, Factor, captureRangeStart, captureRangeEnd); 
                    }
                    ScreenCapture.CaptureScreenshot(uniqueFilename(iterator));                    
                    print(string.Format("Working {0}/{1}", iterator, Iterations));
                }
                else if (iterator < Iterations+1)
                {
                    Factor = Mathf.Lerp(captureRangeStart, captureRangeEnd, (float)iterator / Iterations);
                    PaintingToGraphics();
                    if (showText)
                    {
                        text.text = string.Format("Benjamin Drury Times Table - Points {0,0:D3} Factor {1,0:000.000} Range {2,0:000.000} - {3,0:000.000}", numberOfPoints, Factor, captureRangeStart, captureRangeEnd); 
                    }
                    ScreenCapture.CaptureScreenshot(uniqueFilename(iterator));                                        
                    print(string.Format("Working {0}/{1}", iterator, Iterations));
                }
                else
                {
                    Debug.LogWarning("Done!");
                    EditorApplication.isPlaying = false;
                }
                iterator += 1;
                break;
            case CaptureType.RenderRangePoints:
                if (iterator == 0)
                {
                    numberOfPoints = Mathf.RoundToInt(captureRangeStart);
                    UpdatePointPositions();
                    SetupNumberOfPoints();
                    PaintingToGraphics();
                    if (showText)
                    {
                        text.text = string.Format("Benjamin Drury Times Table - Points {0,0:D3} Factor {1,0:000.000} Range {2,0:D3} - {3,0:D3}", numberOfPoints, Factor, Mathf.RoundToInt(captureRangeStart), Mathf.RoundToInt(captureRangeEnd)); 
                    }
                    ScreenCapture.CaptureScreenshot(uniqueFilename(iterator));
                    print(string.Format("Working {0}/{1}", iterator, Iterations));
                }
                else if (iterator < Iterations + 1)
                {
                    numberOfPoints = Mathf.RoundToInt(Mathf.Lerp(captureRangeStart, captureRangeEnd, (float)iterator / Iterations));
                    UpdatePointPositions();
                    SetupNumberOfPoints();
                    PaintingToGraphics();
                    if (showText)
                    {
                        text.text = string.Format("Benjamin Drury Times Table - Points {0,0:D3} Factor {1,0:000.000} Range {2,0:D3} - {3,0:D3}", numberOfPoints, Factor, Mathf.RoundToInt(captureRangeStart), Mathf.RoundToInt(captureRangeEnd)); 
                    }
                    ScreenCapture.CaptureScreenshot(uniqueFilename(iterator));
                    print(string.Format("Working {0}/{1}", iterator, Iterations));
                }
                else
                {
                    Debug.LogWarning("Done!");
                    EditorApplication.isPlaying = false;
                }
                iterator += 1;
                break;           
        }
    }

    void AutoUpdateFactor()
    {
        if (runAutomaticaly)
        {
            if (countdownToStart > 0)
            {
                countdownToStart -= Time.deltaTime;             
            }
            else
            {
                timeCounter += Time.deltaTime;
            }

            factor = Mathf.Lerp(RangeStart, RangeEnd, timeCounter / secondsForRotation);

            if (timeCounter >= secondsForRotation)
            {
                if (loop)
                {
                    timeCounter -= secondsForRotation;
                }
                else
                {
                    timeCounter = 0;
                    runAutomaticaly = false;
                }
            }
        }
    }

    void PaintingToGraphics()
    {
        if (showLines)
        {
            RepaintMeshFactorLines();            
        }

        if (showOutlines)
        {
            RepaintNumberOfPoints();            
        }

        if (showFactorPosition)
        {
            RepaintScrollingPoint();            
        }
}

    #region Checking    

    [ContextMenu("Compute Valid Positions")]
    void SumNonZeroPoints()
    {
        int validPositions = 0;

        for (int f = 1; f < numberOfPoints; f++)
        {
            for (int p = 0; p < numberOfPoints; p++)
            {
                if (Mathf.FloorToInt(p * f) % numberOfPoints == 0)
                {
                    Debug.LogFormat("Position {0} Times Function {1} = 0", p, f);
                }
                else
                {
                    validPositions++;
                }
            }
        }

        Debug.LogFormat("There are a total of {0} valid positions in a {1} Times Table", validPositions, numberOfPoints);
    }

    #endregion
    
    #region Matrix Work

    void SetupStaticMatrix()
    {
        staticSimpleMatrixPoint = SimpleMatrix(Vector3.zero);
    }

    #region Matrix Methods
    
    Matrix4x4 SimpleMatrix(Vector3 position)
    {
        Matrix4x4 point = new Matrix4x4();
        point.SetTRS(position, Quaternion.identity, Vector3.one);
        return point;
    }

    Matrix4x4 SimpleMatrix(Vector3 position, Vector3 scale)
    {
        Matrix4x4 point = new Matrix4x4();
        point.SetTRS(position, Quaternion.identity, scale);
        return point;
    }

    Matrix4x4 SimpleMatrix(Vector3 position, float scale)
    {
        Matrix4x4 point = new Matrix4x4();
        point.SetTRS(position, Quaternion.identity, new Vector3(scale, scale, scale));
        return point;
    }

    Matrix4x4 SimpleMatrix(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Matrix4x4 point = new Matrix4x4();
        point.SetTRS(position, rotation, scale);
        return point;
    }
    #endregion

    #endregion

    #region Mesh Work

    #region points
    void SetupNumberOfPoints()
    {
        pointMaterialBlock = new MaterialPropertyBlock[numberOfPoints];
        meshPointLines = new Mesh[numberOfPoints];
        for (int i = 0; i < numberOfPoints; i++)
        {
            pointMaterialBlock[i] = new MaterialPropertyBlock();

            switch (lineColour)
            {
                case ColourState.global:
                    pointMaterialBlock[i].SetColor("_Color", globalLineColour);
                    break;
                case ColourState.independent:
                    pointMaterialBlock[i].SetColor("_Color", ColourPicker(i, numberOfPoints));
                    break;
                case ColourState.directionModLocal:
                    pointMaterialBlock[i].SetColor("_Color", Color.magenta);
                    break;
                case ColourState.directionModGlobal:
                    pointMaterialBlock[i].SetColor("_Color", Color.magenta);
                    break;
            }

            

            meshPointLines[i] = GetMeshForRing(i);            
        }
    }

    void RepaintNumberOfPoints()
    {
        for (int i = 0; i < numberOfPoints; i++)
        {
            RepaintPoint(i);
        }
    }

    void RepaintPoint(int i)
    {
        Graphics.DrawMesh(meshPointLines[i], staticSimpleMatrixPoint, mat, 1, null, 0, pointMaterialBlock[i]); // Draw Lines Between point origins        
    }
    #endregion
    
    #region Factor Lines
    
    void RepaintMeshFactorLines()
    {
        for (int i = 0; i < numberOfPoints; i++)
        {
            RepaintFactorLine(i);
        }
    }

    void RepaintFactorLine(int i)
    {
        Graphics.DrawMesh(GetMeshForLine(i, Factor), staticSimpleMatrixPoint, mat, 0, null, 0, pointMaterialBlock[i]); // Draw Lines between Factor Points        
    }
    
    #endregion

    #region Generate Mesh

    Mesh GetMeshForRing(int index)
    {
        return MakeQuad(PointFromAngle(index, radius), PointFromAngle((index + 1) % numberOfPoints, radius));
    }

    Mesh GetMeshForSpoke(float index, float Radius)
    {
        return MakeQuad(PointFromAngle(index, radius), PointFromAngle(index, Radius));
    }

    Mesh GetMeshForLine(int index,float f)
    {
        return MakeQuad(PointFromAngle(index, radius), PointFromAngle((Mathf.RoundToInt(f * index) % numberOfPoints), radius));
    }

    Mesh MakeQuad(Vector3 start, Vector3 end)
    {
        Mesh tempMesh = new Mesh();

        start.z -= start.z;
        end.z -= end.z;

        // verts
        Vector3 normal = Vector3.forward;
        Vector3 side = Vector3.Cross((end - start).normalized, Vector3.forward);
        side.Normalize();

        Vector3[] verts = new Vector3[4];
        verts[0] = start + side * (lineWidth / 2);
        verts[1] = start + side * (lineWidth / -2);
        verts[2] = end + side * (lineWidth / 2);
        verts[3] = end + side * (lineWidth / -2);

        // tris
        int[] tri = new int[6];
        tri[0] = 0;
        tri[1] = 2;
        tri[2] = 1;
        tri[3] = 2;
        tri[4] = 3;
        tri[5] = 1;

        // normals
        Vector3[] normals = new Vector3[4];
        normals[0] = normal;
        normals[1] = normal;
        normals[2] = normal;
        normals[3] = normal;

        // uvs
        //Vector2[] uv = new Vector2[4];
        //uv[0] = new Vector2(0, 0);
        //uv[1] = new Vector2(1, 0);
        //uv[2] = new Vector2(0, 1);
        //uv[3] = new Vector2(1, 1);

        // assign
        tempMesh.vertices = verts;
        tempMesh.triangles = tri;
        //tempMesh.normals = normals;
        //tempMesh.uv = uv;

        return tempMesh;
    }
    #endregion

    #region Scrolling Point

    void SetupScrollingPoint()
    {
        scrollingPointMaterialBlock = new MaterialPropertyBlock();
    }

    void RepaintScrollingPoint()
    {   
        scrollingPointMaterialBlock.SetColor("_Color", ColourPicker(Factor, numberOfPoints));

        Graphics.DrawMesh(GetMeshForSpoke(Factor, radius * 1.1f), staticSimpleMatrixPoint, mat, 0, null, 0, scrollingPointMaterialBlock);

        Graphics.DrawMesh(pointMesh, SimpleMatrix(PointFromAngle(Factor, radius * 1.1f)), mat, 1, null, 0, scrollingPointMaterialBlock);
    }

    #endregion

    #endregion

    #region Calculate Points
    [ContextMenu("Update Points")]
    void UpdatePointPositions()
    {
        points = new Vector3[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            points[i] = PointFromAngle(i, radius);
        }

    }

    Vector3 PointFromAngle(float point, float radius)
    {
        float angle = Increment * point; // float Increment { get { return (360 * persentageOfCircle) / numberOfPoints; } }
        float rads = angle * halfPI; // float halfPI { get { return Mathf.PI / 180; } } 

        return new Vector3(offset.x + radius * Mathf.Cos(rads), offset.y + radius * Mathf.Sin(rads), 0);
    }
    #endregion
    
    #region Colours
    Color ColourPicker(float i, int iMax)
    {
        return Color.HSVToRGB(i / iMax, 1f, 1f);
    }
    #endregion

    #region Draw With Gizmos
    void DrawLinesFromPoints()
    {
        if (points.Length > 0 && showLines)
        {
            for (int i = 1; i < numberOfPoints; i++)
            {
                Vector3 start = points[i];
                Vector3 end = points[Mathf.RoundToInt(Factor * i) % numberOfPoints];                
                switch (lineColour)
                {
                    case ColourState.global:
                        Gizmos.color = ColourPicker(Factor, numberOfPoints);
                        Gizmos.DrawLine(start, end);
                        break;
                    case ColourState.independent:
                        Gizmos.color = ColourPicker(i, numberOfPoints);
                        Gizmos.DrawLine(start, end);
                        break;
                    case ColourState.directionModLocal:
                        Vector3 localUp = Vector3.Cross(start, end).normalized;
                        Vector3 localRight = Vector3.Cross((end-start).normalized,localUp);

                        Gizmos.color = Color.blue;
                        Gizmos.DrawLine(start, end);

                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(start, start + localUp);

                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(start, start + localRight);
                        break;
                    case ColourState.directionModGlobal:
                        
                        Vector3 globalRight = Vector3.Cross((end - start).normalized, Vector3.forward);

                        Gizmos.color = Color.blue;
                        Gizmos.DrawLine(start, end);

                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(start, start + Vector3.forward);

                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(start, start + globalRight);
                        break;
                    default:
                        break;
                }
            }
        }
    }

    void DrawPositionsFromPoints()
    {
        if (points.Length > 0 && showOutlines)
        {
            {
                Gizmos.color = ColourPicker(Factor, numberOfPoints);
                Gizmos.DrawSphere(PointFromAngle(Factor, radius + (pointSize * 6)), pointSize * 3);
            }

            for (int i = 0; i < points.Length; i++)
            {
                Gizmos.color = ColourPicker(i, points.Length);
                Gizmos.DrawSphere(points[i], pointSize);
            }
        }
    } 
    #endregion

    private void OnDrawGizmos()
    {
        DrawLinesFromPoints();

        DrawPositionsFromPoints();
        
    }

    #region Editor Methods
    
    public void Capture()
    {        
        switch (captureType)
        {
            case CaptureType.EditorScreenshot:
                string output = uniqueFilename();
                ScreenCapture.CaptureScreenshot(output);
                print(output);
                break;
            case CaptureType.RenderSingleShot:                
            case CaptureType.RenderRangeFactor:
            case CaptureType.RenderRangePoints:
                StartCaptureLive();
                break;
            default:
                break;
        }
    }
       
    bool captureDone;
    
    void StartCaptureLive()
    {
        runAutomaticaly = false;
        EditorApplication.isPlaying = true;
    }

    //public static string ScreenShotName(int width, int height)
    //{
    //    return string.Format("{0}/screenshots/screen_{1}x{2}_{3}.png",
    //                         Application.dataPath,
    //                         width, height,
    //                         System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    //}

    //public void HiResShot()
    //{
    //    RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
    //    shotCamera.targetTexture = rt;
    //    Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
    //    shotCamera.Render();
    //    RenderTexture.active = rt;
    //    screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
    //    shotCamera.targetTexture = null;
    //    RenderTexture.active = null; // JC: added to avoid errors
    //    DestroyImmediate(rt);
    //    byte[] bytes = screenShot.EncodeToJPG();        
    //    string filename = uniqueFilename();
    //    System.IO.File.WriteAllBytes(filename, bytes);
    //    Debug.Log(string.Format("Took screenshot to: {0}", filename));
    //}

    private string uniqueFilename(int width, int height)
    {
        // if folder not specified by now use a good default
        if (folder == null || folder.Length == 0)
        {
            folder = Application.dataPath;
            if (Application.isEditor)
            {
                // put screenshots in folder above asset path so unity doesn't index the files
                var stringPath = folder + "/..";
                folder = Path.GetFullPath(stringPath);
            }
            folder += "/screenshots";

            // make sure directoroy exists
            System.IO.Directory.CreateDirectory(folder);

            // count number of files of specified format in folder
            string mask = string.Format("screen_{0}x{1}*.{2}", width, height, "png");
            counter = Directory.GetFiles(folder, mask, SearchOption.TopDirectoryOnly).Length;
        }

        // use width, height, and counter for unique file name
        var filename = string.Format("{0}/screen_{1}x{2}_{3}.{4}", folder, width, height, counter, "png");

        // up counter for next call
        ++counter;

        // return unique filename
        return filename;
    }

    private string uniqueFilename()
    {        
        // use width, height, and counter for unique file name
        var filename = string.Format("{0}/Times Table - Points {1,0:D3} - Factor {2,0:000.000}.{3}", folder, numberOfPoints,Factor, "png");

        // return unique filename
        return filename;
    }

    private string uniqueFilename(int sequinceNumber)
    {
        // use width, height, and counter for unique file name
        var filename = string.Format("{0}/Times Table - Points {1,0:D3} - Range{2,0:000.000}-{3,0:000.000} - {4,0:D2}fps - {5,0:D3}.{6}", folder, numberOfPoints, captureRangeStart, captureRangeEnd,framerate, sequinceNumber, "png");        

        // return unique filename
        return filename;
    }
    #endregion
}
