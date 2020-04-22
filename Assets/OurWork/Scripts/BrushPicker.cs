using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class BrushPicker : MonoBehaviour
{
    //public CS_BAXTER m_brushScript;
    public Baxter3D m_brushScript;
    public Material m_pressureMat;
    public Material m_angleMat;

    public List<Brush> m_brushes;

    public float angleSpeed = 4.0f;
    public float pressureSpeed = 0.3f;

    [SerializeField()]
    private BrushTex m_brush1;
    [SerializeField()]
    private BrushTex m_brush2;

    private RenderTexture m_angledBrush;

    public int m_activeBrush = 0;
    public int m_activeTexture = 0;


    [Range(0.5f, 1.0f)]
    public float m_pressure = 0.5f;

    [Range(0.0f, 90.0f)]
    public float m_angle = 90.0f;

    [SerializeField]
    private Texture2D m_brushToUseTexture;
    private RenderTexture m_brushToUse;

    public Text pressureVal;
    public Text angleVal;


    RenderTexture CreateRenderTexture(int w, int h, int type = 0)
    {
        var format = RenderTextureFormat.ARGBFloat;
        if (type == 1) format = RenderTextureFormat.RFloat;

        RenderTexture theTex;
        theTex = new RenderTexture(w, h, 0, format);
        theTex.enableRandomWrite = true;
        theTex.Create();
        return theTex;
    }

    void InitRenderTex(int w, int h)
    {
        m_brushToUse = CreateRenderTexture(w, h);
        m_angledBrush = CreateRenderTexture(w, h);
    }

    private void ApplyPressure()
    {
        float brightnessVal = m_pressure * 3;

        m_pressureMat.SetFloat("_BrightnessAmount", brightnessVal);

        //Blits the active brush texture to the brush to use render texture
        Graphics.Blit(m_angledBrush, m_brushToUse, m_pressureMat);
    }

    private void ApplyAngle()
    {
        m_angleMat.SetTexture("_AngleTex1", m_brush1.m_texture);
        m_angleMat.SetTextureOffset("_AngleTex1", m_brush1.m_center);
        m_angleMat.SetTexture("_AngleTex2", m_brush2.m_texture);
        m_angleMat.SetTextureOffset("_AngleTex2", m_brush2.m_center);
        m_angleMat.SetFloat("_Angle1", m_brush1.m_angle);
        m_angleMat.SetFloat("_Angle2", m_brush2.m_angle);
        m_angleMat.SetFloat("_CurrentAngle", m_angle);

        Graphics.Blit(m_brush1.m_texture, m_angledBrush, m_angleMat);
    }

    private void UpdateBrush()
    {
        float lastAngle = 0.0f;
        for (int i = 0; i < m_brushes[m_activeBrush].m_strokes.Count; i++)
        {
            float angle = m_brushes[m_activeBrush].m_strokes[i].m_angle;

            if (m_angle >= angle && m_angle <= lastAngle)
            {
                m_brush1 = m_brushes[m_activeBrush].m_strokes[i];
            }

            if (m_angle <= angle)
            {
                m_brush2 = m_brushes[m_activeBrush].m_strokes[i];
            }

            lastAngle = angle;
        }

        ApplyAngle();

        ApplyPressure();

        //Sets brush to use as active
        RenderTexture.active = m_brushToUse;

        //Reads the pixels from brush to use because the compute shader requires a texture2D
        m_brushToUseTexture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
        //Applies it
        m_brushToUseTexture.Apply();

        //Gives the current brush that is to be used, to the Baxter script
        m_brushScript.initialBrush = m_brushToUseTexture;
    }

    private void Start()
    {
        //Formats texture
        m_brushToUseTexture = new Texture2D(256, 256, TextureFormat.RGB24, false);

        m_brushScript.initialBrush = m_brushes[m_activeBrush][m_activeTexture];

        InitRenderTex(m_brushScript.initialBrush.width, m_brushScript.initialBrush.height);

        //Update brush once intially.
        UpdateBrush();
    }

    void KeyboardControls()
    {
        if (Input.GetKey(KeyCode.O))
        {
            m_pressure = m_pressure - (pressureSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.P))
        {
            m_pressure = m_pressure + (pressureSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.Minus))
        {
            m_angle = m_angle - (angleSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.Equals))
        {
            m_angle = m_angle + (angleSpeed * Time.deltaTime);
        }

        m_angle = Mathf.Clamp(m_angle, 0.0f, 90.0f);
        m_pressure = Mathf.Clamp(m_pressure, 0.0f, 1.0f);
    }

    void UpdateNumDisplay()
    {
        if (pressureVal != null)
        {
            pressureVal.text = (m_pressure * 100.0f).ToString() + "%";
        }
        if (angleVal != null)
        {
            angleVal.text = m_angle.ToString() + " degrees";
        }    
    }

    // Update is called once per frame
    void Update()
    {
        UpdateBrush();

        KeyboardControls();

        UpdateNumDisplay();
    }
}
