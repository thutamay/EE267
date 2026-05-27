using UnityEngine;

public class AmesRoomBuilder : MonoBehaviour
{
    [Header("Editor controls")]
    public bool rebuildNow = false;

    [Header("Museum layout")]
    public Vector3 roomOffset = new Vector3(0f, 0f, 8f);
    public float museumHalfWidth = 25f;
    public float museumFront = -10f;
    public float museumBack = 30f;

    [Header("Ames Window exhibit")]
    public Vector3 amesWindowPosition = new Vector3(7.5f, 0f, 8f);
    public float amesWindowScale = 1.0f;
    public float amesWindowRotationSpeed = 30f;

    [Header("Material overrides")]
    public Material museumFloorOverride;

    [Header("Camera / viewpoint")]
    public float cameraFov = 55f;
    public Vector3 viewpointSpotPosition = new Vector3(-0.65f, 0.02f, -4.8f);

    [Header("Figure settings")]
    public float figureHeight = 1.4f;
    public float figureRadius = 0.18f;

    private Material wallMat;
    private Material rightWallMat;
    private Material backWallMat;
    private Material ceilingMat;
    private Material redMat;
    private Material blueMat;
    private Material museumFloorMat;
    private Material markerMat;

    private void Start()
    {
        BuildMuseum();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!rebuildNow)
        {
            return;
        }

        rebuildNow = false;
        UnityEditor.EditorApplication.delayCall += RebuildFromEditorDelay;
#endif
    }

#if UNITY_EDITOR
    private void RebuildFromEditorDelay()
    {
        if (this == null)
        {
            return;
        }

        BuildMuseum();
    }
#endif

    public void BuildMuseum()
    {
        ClearChildren();
        CreateMaterials();

        BuildMuseumGround();
        BuildViewpointSpot();
        BuildAmesRoom(roomOffset);
        BuildAmesWindowExhibit(amesWindowPosition);

        SetupCamera();
    }

    private void BuildMuseumGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Museum Ground";
        ground.transform.SetParent(transform);

        float width = museumHalfWidth * 2f;
        float depth = museumBack - museumFront;

        ground.transform.position = new Vector3(0f, 0f, (museumFront + museumBack) / 2f);
        ground.transform.localScale = new Vector3(width / 10f, 1f, depth / 10f);

        MeshCollider meshCollider = ground.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = ground.AddComponent<MeshCollider>();
        }
        meshCollider.convex = false;
        meshCollider.isTrigger = false;

        BoxCollider boxCollider = ground.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = ground.AddComponent<BoxCollider>();
        }
        boxCollider.center = new Vector3(0f, -0.05f, 0f);
        boxCollider.size = new Vector3(10f, 0.1f, 10f);
        boxCollider.isTrigger = false;

        Renderer renderer = ground.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = museumFloorMat;
        }
    }

    private void BuildViewpointSpot()
    {
        GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spot.name = "Viewpoint Spot";
        spot.transform.SetParent(transform);
        spot.transform.position = viewpointSpotPosition;
        spot.transform.localScale = new Vector3(0.6f, 0.02f, 0.6f);
        spot.GetComponent<Renderer>().sharedMaterial = markerMat;
    }

    private void BuildAmesRoom(Vector3 offset)
    {
        Vector3 eye = new Vector3(-0.65f, 1.6256f, -4.8f);

        float frontZ = offset.z;
        float apparentBackZ = offset.z + 7.5f;

        float leftActualBackZ = offset.z + 9.0f;
        float rightActualBackZ = offset.z + 4.8f;

        float frontHalfWidth = 2.65f;
        float frontHeight = 3.1f;

        float apparentBackHalfWidth = 2.45f;
        float apparentBackHeight = 2.85f;

        Vector3 frontLeftBottom = new Vector3(-frontHalfWidth, 0f, frontZ);
        Vector3 frontRightBottom = new Vector3(frontHalfWidth, 0f, frontZ);
        Vector3 frontLeftTop = new Vector3(-frontHalfWidth, frontHeight, frontZ);
        Vector3 frontRightTop = new Vector3(frontHalfWidth, frontHeight, frontZ);

        Vector3 apparentBackLeftBottom = new Vector3(-apparentBackHalfWidth, 0f, apparentBackZ);
        Vector3 apparentBackRightBottom = new Vector3(apparentBackHalfWidth, 0f, apparentBackZ);
        Vector3 apparentBackLeftTop = new Vector3(-apparentBackHalfWidth, apparentBackHeight, apparentBackZ);
        Vector3 apparentBackRightTop = new Vector3(apparentBackHalfWidth, apparentBackHeight, apparentBackZ);

        Vector3 backLeftBottom = ProjectPointToZ(eye, apparentBackLeftBottom, leftActualBackZ);
        Vector3 backRightBottom = ProjectPointToZ(eye, apparentBackRightBottom, rightActualBackZ);
        Vector3 backLeftTop = ProjectPointToZ(eye, apparentBackLeftTop, leftActualBackZ);
        Vector3 backRightTop = ProjectPointToZ(eye, apparentBackRightTop, rightActualBackZ);

        MakeDoubleSidedQuad("Ames Ceiling", frontLeftTop, backLeftTop, backRightTop, frontRightTop, ceilingMat);
        MakeDoubleSidedQuad("Ames Left Wall", frontLeftBottom, backLeftBottom, backLeftTop, frontLeftTop, wallMat);
        MakeDoubleSidedQuad("Ames Right Wall", frontRightBottom, frontRightTop, backRightTop, backRightBottom, rightWallMat);
        MakeDoubleSidedQuad("Ames Back Wall", backLeftBottom, backRightBottom, backRightTop, backLeftTop, backWallMat);

        Vector3 blueBase = Vector3.Lerp(backLeftBottom, backRightBottom, 0.10f);
        blueBase = Vector3.Lerp(blueBase, frontLeftBottom, 0.08f);
        blueBase.x += 0.12f;
        blueBase.y = 0.02f;

        Vector3 redBase = Vector3.Lerp(backRightBottom, frontRightBottom, 0.22f);
        redBase.x -= 0.28f;
        redBase.z -= 0.08f;
        redBase.y = 0.02f;

        MakeFigure("Far Blue Figure", blueBase, blueMat);
        MakeFigure("Near Red Figure", redBase, redMat);
    }

    private Vector3 ProjectPointToZ(Vector3 eye, Vector3 apparentPoint, float targetZ)
    {
        float t = (targetZ - eye.z) / (apparentPoint.z - eye.z);
        return eye + t * (apparentPoint - eye);
    }



    private void BuildAmesWindowExhibit(Vector3 position)
    {
        GameObject root = new GameObject("Ames Window Exhibit");
        root.transform.SetParent(transform);
        root.transform.position = position;

        Material frontFrameMat = MakeMaterial("Ames Window White Frame Material", Color.white);
        Material sideFrameMat = MakeMaterial("Ames Window Gray Thickness Material", new Color(0.43f, 0.45f, 0.45f));
        Material rulerMat = MakeMaterial("Ames Window Ruler Wood Material", new Color(0.62f, 0.38f, 0.16f));

        GameObject rotatingWindow = new GameObject("Rotating Procedural Ames Window");
        rotatingWindow.transform.SetParent(root.transform);
        rotatingWindow.transform.localPosition = new Vector3(0f, 1.9f, 0f);
        rotatingWindow.transform.localRotation = Quaternion.identity;
        rotatingWindow.transform.localScale = Vector3.one * amesWindowScale;

        AmesWindowRotator rotator = rotatingWindow.AddComponent<AmesWindowRotator>();
        rotator.rotationSpeedDegreesPerSecond = amesWindowRotationSpeed;

        BuildAmesWindowSlabWithOpenings(rotatingWindow.transform, frontFrameMat, sideFrameMat);
        BuildAmesWindowRuler(rotatingWindow.transform, rulerMat);
    }

    private void BuildAmesWindowRuler(Transform parent, Material rulerMat)
    {
        GameObject ruler = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ruler.name = "Ames Window Ruler";
        ruler.transform.SetParent(parent);
        ruler.transform.localPosition = new Vector3(-0.55f, 0.36f, 0f);
        ruler.transform.localRotation = Quaternion.Euler(0f, -18f, 0f);
        ruler.transform.localScale = new Vector3(0.085f, 0.085f, 5.8f);
        ruler.GetComponent<Renderer>().sharedMaterial = rulerMat;

        Collider rulerCollider = ruler.GetComponent<Collider>();
        if (rulerCollider != null)
        {
            SafeDestroy(rulerCollider);
        }

    }

    private void BuildAmesWindowSlabWithOpenings(Transform parent, Material frontMat, Material sideMat)
    {
        float outerLeft = -2.25f;
        float outerRight = 2.25f;
        float frontZ = 0.045f;
        float backZ = -0.045f;

        float[] openingLefts = { -1.95f, -1.08f, 0.28f };
        float[] openingRights = { -1.35f, -0.12f, 1.55f };
        float bottomOpenMin = 0.17f;
        float bottomOpenMax = 0.43f;
        float topOpenMin = 0.57f;
        float topOpenMax = 0.83f;

        CreateAmesWindowWhiteFacePanels(parent, frontMat, outerLeft, outerRight, openingLefts, openingRights, bottomOpenMin, bottomOpenMax, topOpenMin, topOpenMax, frontZ, "Front Face");
        CreateAmesWindowWhiteFacePanels(parent, frontMat, outerLeft, outerRight, openingLefts, openingRights, bottomOpenMin, bottomOpenMax, topOpenMin, topOpenMax, backZ, "Back Face");

        CreateAmesWindowOuterDepth(parent, sideMat, outerLeft, outerRight, frontZ, backZ);

        for (int i = 0; i < openingLefts.Length; i++)
        {
            CreateAmesWindowOpeningDepth("Top Opening " + (i + 1), parent, sideMat, openingLefts[i], openingRights[i], topOpenMin, topOpenMax, frontZ, backZ);
            CreateAmesWindowOpeningDepth("Bottom Opening " + (i + 1), parent, sideMat, openingLefts[i], openingRights[i], bottomOpenMin, bottomOpenMax, frontZ, backZ);
            CreateAmesWindowPaintedOpeningTrim("Top Opening " + (i + 1) + " Front", parent, sideMat, openingLefts[i], openingRights[i], topOpenMin, topOpenMax, frontZ + 0.003f, true, false);
            CreateAmesWindowPaintedOpeningTrim("Bottom Opening " + (i + 1) + " Front", parent, sideMat, openingLefts[i], openingRights[i], bottomOpenMin, bottomOpenMax, frontZ + 0.003f, false, true);
            CreateAmesWindowPaintedOpeningTrim("Top Opening " + (i + 1) + " Back", parent, sideMat, openingLefts[i], openingRights[i], topOpenMin, topOpenMax, backZ - 0.003f, true, false);
            CreateAmesWindowPaintedOpeningTrim("Bottom Opening " + (i + 1) + " Back", parent, sideMat, openingLefts[i], openingRights[i], bottomOpenMin, bottomOpenMax, backZ - 0.003f, false, true);
        }
    }

    private Vector3 AmesWindowPoint(float x, float heightFraction, float z)
    {
        float t = Mathf.InverseLerp(-2.25f, 2.25f, x);
        float topY = Mathf.Lerp(0.72f, 1.25f, t);
        float bottomY = Mathf.Lerp(-0.72f, -1.25f, t);
        return new Vector3(x, Mathf.Lerp(bottomY, topY, heightFraction), z);
    }

    private void CreateAmesWindowWhiteFacePanels(Transform parent, Material mat, float outerLeft, float outerRight, float[] openingLefts, float[] openingRights, float bottomOpenMin, float bottomOpenMax, float topOpenMin, float topOpenMax, float z, string label)
    {
        CreateAmesWindowFacePanel("Ames Window Bottom " + label, parent, outerLeft, outerRight, 0f, bottomOpenMin, z, mat);
        CreateAmesWindowFacePanel("Ames Window Middle " + label, parent, outerLeft, outerRight, bottomOpenMax, topOpenMin, z, mat);
        CreateAmesWindowFacePanel("Ames Window Top " + label, parent, outerLeft, outerRight, topOpenMax, 1f, z, mat);

        CreateAmesWindowFacePanel("Ames Window Left " + label, parent, outerLeft, openingLefts[0], 0f, 1f, z, mat);
        CreateAmesWindowFacePanel("Ames Window Divider 1 " + label, parent, openingRights[0], openingLefts[1], 0f, 1f, z, mat);
        CreateAmesWindowFacePanel("Ames Window Divider 2 " + label, parent, openingRights[1], openingLefts[2], 0f, 1f, z, mat);
        CreateAmesWindowFacePanel("Ames Window Right " + label, parent, openingRights[2], outerRight, 0f, 1f, z, mat);
    }

    private void CreateAmesWindowPaintedOpeningTrim(string name, Transform parent, Material mat, float x0, float x1, float y0Fraction, float y1Fraction, float z, bool paintTop, bool paintBottom)
    {
        float sideWidth = 0.1f;
        float railHeight = 0.035f;

        CreateAmesWindowFacePanel("Ames Window " + name + " Left Painted Shadow", parent, x0, Mathf.Min(x0 + sideWidth, x1), y0Fraction, y1Fraction, z, mat);
        if (paintTop)
        {
            CreateAmesWindowFacePanel("Ames Window " + name + " Top Painted Shadow", parent, x0, x1, Mathf.Max(y1Fraction - railHeight, y0Fraction), y1Fraction, z, mat);
        }

        if (paintBottom)
        {
            CreateAmesWindowFacePanel("Ames Window " + name + " Bottom Painted Shadow", parent, x0, x1, y0Fraction, Mathf.Min(y0Fraction + railHeight, y1Fraction), z, mat);
        }
    }

    private void CreateAmesWindowFacePanel(string name, Transform parent, float x0, float x1, float y0Fraction, float y1Fraction, float z, Material mat)
    {
        CreateTrapezoidPane(
            name,
            parent,
            AmesWindowPoint(x0, y0Fraction, z),
            AmesWindowPoint(x1, y0Fraction, z),
            AmesWindowPoint(x1, y1Fraction, z),
            AmesWindowPoint(x0, y1Fraction, z),
            mat);
    }

    private void CreateAmesWindowOpeningDepth(string name, Transform parent, Material mat, float x0, float x1, float y0Fraction, float y1Fraction, float frontZ, float backZ)
    {
        Vector3 frontBottomLeft = AmesWindowPoint(x0, y0Fraction, frontZ);
        Vector3 frontBottomRight = AmesWindowPoint(x1, y0Fraction, frontZ);
        Vector3 frontTopRight = AmesWindowPoint(x1, y1Fraction, frontZ);
        Vector3 frontTopLeft = AmesWindowPoint(x0, y1Fraction, frontZ);

        Vector3 backBottomLeft = AmesWindowPoint(x0, y0Fraction, backZ);
        Vector3 backBottomRight = AmesWindowPoint(x1, y0Fraction, backZ);
        Vector3 backTopRight = AmesWindowPoint(x1, y1Fraction, backZ);
        Vector3 backTopLeft = AmesWindowPoint(x0, y1Fraction, backZ);

        CreateTrapezoidPane("Ames Window " + name + " Left Thickness", parent, frontBottomLeft, backBottomLeft, backTopLeft, frontTopLeft, mat);
        CreateTrapezoidPane("Ames Window " + name + " Right Thickness", parent, backBottomRight, frontBottomRight, frontTopRight, backTopRight, mat);
        CreateTrapezoidPane("Ames Window " + name + " Top Thickness", parent, frontTopLeft, backTopLeft, backTopRight, frontTopRight, mat);
        CreateTrapezoidPane("Ames Window " + name + " Bottom Thickness", parent, backBottomLeft, frontBottomLeft, frontBottomRight, backBottomRight, mat);
    }

    private void CreateAmesWindowOuterDepth(Transform parent, Material mat, float outerLeft, float outerRight, float frontZ, float backZ)
    {
        CreateTrapezoidPane("Ames Window Outer Left Thickness", parent, AmesWindowPoint(outerLeft, 0f, backZ), AmesWindowPoint(outerLeft, 0f, frontZ), AmesWindowPoint(outerLeft, 1f, frontZ), AmesWindowPoint(outerLeft, 1f, backZ), mat);
        CreateTrapezoidPane("Ames Window Outer Right Thickness", parent, AmesWindowPoint(outerRight, 0f, frontZ), AmesWindowPoint(outerRight, 0f, backZ), AmesWindowPoint(outerRight, 1f, backZ), AmesWindowPoint(outerRight, 1f, frontZ), mat);
        CreateTrapezoidPane("Ames Window Outer Top Thickness", parent, AmesWindowPoint(outerLeft, 1f, frontZ), AmesWindowPoint(outerRight, 1f, frontZ), AmesWindowPoint(outerRight, 1f, backZ), AmesWindowPoint(outerLeft, 1f, backZ), mat);
        CreateTrapezoidPane("Ames Window Outer Bottom Thickness", parent, AmesWindowPoint(outerRight, 0f, frontZ), AmesWindowPoint(outerLeft, 0f, frontZ), AmesWindowPoint(outerLeft, 0f, backZ), AmesWindowPoint(outerRight, 0f, backZ), mat);
    }

    private void CreateTrapezoidPane(string name, Transform parent, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { a, b, c, d };
        mesh.triangles = new int[]
        {
            0, 1, 2,
            0, 2, 3,
            0, 2, 1,
            0, 3, 2
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter filter = go.AddComponent<MeshFilter>();
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        filter.sharedMesh = mesh;
        renderer.sharedMaterial = mat;
    }

    private void CreateLocalBeam(string name, Transform parent, Vector3 start, Vector3 end, float thickness, Material mat)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;

        if (length <= 0.0001f)
        {
            return;
        }

        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beam.name = name;
        beam.transform.SetParent(parent);

        beam.transform.localPosition = (start + end) * 0.5f;
        beam.transform.localRotation = Quaternion.FromToRotation(Vector3.right, direction.normalized);
        beam.transform.localScale = new Vector3(length, thickness, thickness);

        Collider collider = beam.GetComponent<Collider>();
        if (collider != null)
        {
            SafeDestroy(collider);
        }

        beam.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private void MakeFigure(string name, Vector3 basePosition, Material mat)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = name;
        body.transform.SetParent(transform);
        body.transform.position = basePosition + new Vector3(0f, figureHeight / 2f, 0f);
        body.transform.localScale = new Vector3(figureRadius * 2f, figureHeight / 2f, figureRadius * 2f);
        body.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = name + " Head";
        head.transform.SetParent(transform);
        head.transform.position = basePosition + new Vector3(0f, figureHeight + 0.22f, 0f);
        head.transform.localScale = new Vector3(0.38f, 0.38f, 0.38f);
        head.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private GameObject MakeDoubleSidedQuad(string name, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { a, b, c, d };

        mesh.triangles = new int[]
        {
            0, 1, 2,
            0, 2, 3,
            0, 2, 1,
            0, 3, 2
        };

        mesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter filter = go.AddComponent<MeshFilter>();
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();

        filter.sharedMesh = mesh;
        renderer.sharedMaterial = mat;

        return go;
    }

    private void SetupCamera()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
        }

        cam.fieldOfView = cameraFov;
        cam.clearFlags = CameraClearFlags.Skybox;
    }

    private void CreateMaterials()
    {
        wallMat = MakeMaterial("Ames Left Wall Material", new Color(0.56f, 0.64f, 0.67f));
        rightWallMat = MakeMaterial("Ames Right Wall Material", new Color(0.46f, 0.54f, 0.57f));
        backWallMat = MakeMaterial("Ames Back Wall Material", new Color(0.66f, 0.72f, 0.74f));
        ceilingMat = MakeMaterial("Ames Ceiling Material", new Color(0.42f, 0.49f, 0.52f));
        ceilingMat = wallMat;
        redMat = MakeMaterial("Red Figure Material", new Color(0.85f, 0.18f, 0.15f));
        blueMat = MakeMaterial("Blue Figure Material", new Color(0.15f, 0.30f, 0.85f));
        markerMat = MakeMaterial("Marker Material", new Color(0.15f, 0.15f, 0.15f));

        museumFloorMat = museumFloorOverride != null
            ? museumFloorOverride
            : MakeMaterial("Museum Floor Material", new Color(0.63f, 0.60f, 0.57f));

    }

    private Material MakeMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material mat = new Material(shader);
        mat.name = name;

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }
        else if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", color);
        }
        else
        {
            mat.color = color;
        }

        if (mat.HasProperty("_Cull"))
        {
            mat.SetFloat("_Cull", 0f);
        }

        return mat;
    }

    private void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }
}
