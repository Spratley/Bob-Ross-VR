using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class testingCShader : MonoBehaviour
{
	public ComputeShader shader;
	public RenderTexture tex,tex2,V,V2,Pressure,density,brush2;
	[SerializeField] public Texture2D initialTexture;
	[SerializeField] public Texture2D initialVelocityTexture;
	[SerializeField] public Texture2D BrushTexture;
	[SerializeField] public Texture2D brush;

	private Renderer rend;
	float dt;
  
    public Camera camera;
    bool MOUSE_DOWN = false;
    bool MOUSE_DOWN_PIGMENT = false;
	Vector3 mousePosition;
	Vector3 mousePositionPrev;
	Vector3 channel;
  	
	int w,h, W2,H2;
    int NumThreadsX, NumThreadsY, NumThreadsZ;
    float DiffusionConstant_a;
	float DiffusionConstant_c;

	static class Kernels
    {
   	 	public const int InitVelocityField = 0;
        public const int Advect = 1;
        public const int AdvectC = 2;
        public const int GaussSeidelIteration = 3;
		public const int Project1_Divergence = 4;
		public const int Project3_ApplyPressure = 5;
		public const int ConvertDensityToColour = 6;
		public const int AddDensityToLocation = 7;
		public const int InitDensity = 8;
		public const int TransferBrushPaint = 9;
    }

     class FluidParameters
    {
    	static public float Viscosity =  0.00005f;// higher is thicker, smaller is thinner 
    	static public float Diffusion = 0.000000001f;
    }

    // Start is called before the first frame update
    void Start()
    {
     	mousePositionPrev = new Vector3(0,0,0);
     	mousePosition = new Vector3(0,0,0);
     	channel = new Vector3(1,0,0);
     	InitializeShader();   
     	
    }

    RenderTexture CreateRenderTexture(int w, int h, int type=0){
    	var format = RenderTextureFormat.ARGBFloat;
    	if(type == 1) format = RenderTextureFormat.RFloat;
    	RenderTexture theTex;
    	theTex = new RenderTexture(w,h,0, format);
    	theTex.enableRandomWrite = true;
    	theTex.Create();
    	return theTex;
    }

	void InitializeShader()
	{
		dt = 0.01f;
		w  = initialTexture.width; h = initialTexture.height;
		W2 = w/2;			  H2 = h/2;
		NumThreadsX = w/8;	NumThreadsY = h/8;	NumThreadsZ = 1;

		tex 	= CreateRenderTexture(w,h);
		tex2 	= CreateRenderTexture(w,h);
		density = CreateRenderTexture(w,h);
		V 		= CreateRenderTexture(w,h);
		V2 		= CreateRenderTexture(w,h);
		Pressure = CreateRenderTexture(w,h,1);
		brush2 		= CreateRenderTexture(w,h);
		
		rend = GetComponent<Renderer>();
		rend.enabled = true;

		brush = new Texture2D(w,h, TextureFormat.RGBA32,false,true);
		//Graphics.Blit(initialTexture, density);

		shader.SetTexture(Kernels.InitDensity,"Colour_in",initialTexture);
		shader.SetTexture(Kernels.InitDensity,"Density_out",density);
	//	shader.SetTexture(Kernels.InitDryness,"DRYNESS",dryness);
 		shader.Dispatch(Kernels.InitDensity, NumThreadsX,NumThreadsY,NumThreadsZ);
	}

	void UpdateShader2(){
		shader.SetFloat("theTime", Time.time);
		shader.SetFloat("dt", dt);
		//brush = Instantiate(BrushTexture);

		if(Input.GetKeyDown("1")) channel = new Vector3(1,0,0);
		if(Input.GetKeyDown("2")) channel = new Vector3(0,1,0);
		if(Input.GetKeyDown("3")) channel = new Vector3(0,0,1);
		if(Input.GetKeyDown("4")) channel = new Vector3(1,1,0);
		
		RaycastHit hit;
		/* set shader variables that they will all use, constants and stuff */
		Ray ray = camera.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out hit)) {
				if(Input.GetMouseButtonDown(0)) MOUSE_DOWN = true;
				if(Input.GetMouseButtonUp(0)) MOUSE_DOWN = false;
				if(Input.GetMouseButtonDown(1)) MOUSE_DOWN_PIGMENT = true;
				if(Input.GetMouseButtonUp(1)) 
				{
					MOUSE_DOWN_PIGMENT = false;
					brush = new Texture2D(w,h, TextureFormat.RGBA32,false,true);
				}
				mousePositionPrev = mousePosition;
				mousePosition = hit.textureCoord;
				
				shader.SetFloat("InputX", hit.textureCoord.x);
				shader.SetFloat("InputY", hit.textureCoord.y);
				float IDX,IDY;
				IDX = hit.textureCoord.x - mousePositionPrev.x;
				IDY = hit.textureCoord.y - mousePositionPrev.y;
				shader.SetFloat("IDX", IDX);
				shader.SetFloat("IDY", IDY);

				
            	if(MOUSE_DOWN_PIGMENT)
            	{
            		int bw = 20;
            		for(int i=-bw;i<bw;i++)
					{
						for(int j=-bw;j<bw;j++)
						{
							Color color = new Color(channel.x,channel.y,channel.z);//Color.blue;
							float width = BrushTexture.width;
							float height = BrushTexture.height;
							int bx = (int)(hit.textureCoord.x*width);
							int by = (int) (hit.textureCoord.y*height);
							brush.SetPixel(bx+i, by+j,color);
						}
					}
            		brush.Apply();


					shader.SetTexture(Kernels.AddDensityToLocation, "Density_in", density);
					shader.SetTexture(Kernels.AddDensityToLocation, "Density_out", density);
					shader.SetTexture(Kernels.AddDensityToLocation, "BrushTexture_in", brush);
					shader.SetTexture(Kernels.AddDensityToLocation, "BrushTexture_out", brush2);
					
					shader.SetVector("Channel", channel);
					shader.Dispatch(Kernels.AddDensityToLocation, NumThreadsX,NumThreadsY, NumThreadsZ);


				}
				if(MOUSE_DOWN || MOUSE_DOWN_PIGMENT)
				{
						shader.SetTexture(Kernels.InitVelocityField,"Velocity_in",V);
						shader.SetTexture(Kernels.InitVelocityField,"Velocity_out",V2);
				 		shader.Dispatch(Kernels.InitVelocityField, NumThreadsX,NumThreadsY, NumThreadsZ);
            			Graphics.CopyTexture(V2,V);
            	}
        

        }

		/* diffuse velocity field */
		for(int i=0;i<2;i++)
		{
			DiffusionConstant_a = dt*FluidParameters.Viscosity*W2*H2;
			DiffusionConstant_c = 1.0f+ 5.0f*DiffusionConstant_a;
			shader.SetFloat("DiffusionConstant_a", DiffusionConstant_a);
			shader.SetFloat("DiffusionConstant_c", DiffusionConstant_c);

			/* ping-pong buffers */
			shader.SetTexture(Kernels.GaussSeidelIteration, "Density_in", V);
			shader.SetTexture(Kernels.GaussSeidelIteration, "Density_out", tex2);
			shader.Dispatch(Kernels.GaussSeidelIteration, NumThreadsX,NumThreadsY, NumThreadsZ);

			shader.SetTexture(Kernels.GaussSeidelIteration, "Density_in", tex2);
			shader.SetTexture(Kernels.GaussSeidelIteration, "Density_out", V);
			shader.Dispatch(Kernels.GaussSeidelIteration, NumThreadsX,NumThreadsY, NumThreadsZ);
		}

		/* advect velocity field */
		shader.SetTexture(Kernels.Advect, "Density_in", V);
		shader.SetTexture(Kernels.Advect, "Velocity_in", V);
		shader.SetTexture(Kernels.Advect, "Density_out", tex2);
		shader.Dispatch(Kernels.Advect, NumThreadsX,NumThreadsY,NumThreadsZ);

		/* project the velocity field */
		shader.SetTexture(Kernels.Project1_Divergence, "Velocity_in", tex2);
		shader.SetTexture(Kernels.Project1_Divergence, "Divergence_out", tex);
		shader.SetTexture(Kernels.Project1_Divergence, "Pressure_out", Pressure);
		shader.Dispatch(Kernels.Project1_Divergence, NumThreadsX,NumThreadsY,NumThreadsZ);

		/* solve for the pressure */
		for(int i=0;i<10;i++)
		{
			DiffusionConstant_a = 1;
			DiffusionConstant_c = 6;
			shader.SetFloat("DiffusionConstant_a", DiffusionConstant_a);
			shader.SetFloat("DiffusionConstant_c", DiffusionConstant_c);
			shader.SetTexture(Kernels.GaussSeidelIteration, "Density_in", tex); // input divergence
			shader.SetTexture(Kernels.GaussSeidelIteration, "Density_out", Pressure); //output pressure
			shader.Dispatch(Kernels.GaussSeidelIteration, NumThreadsX,NumThreadsY, NumThreadsZ);
		}

		/* now finish the pressure projection */
		shader.SetTexture(Kernels.Project3_ApplyPressure, "Pressure_in", Pressure);
		shader.SetTexture(Kernels.Project3_ApplyPressure, "Velocity_in", V);
		shader.SetTexture(Kernels.Project3_ApplyPressure, "Velocity_out", tex2);
		shader.Dispatch(Kernels.Project3_ApplyPressure, NumThreadsX,NumThreadsY, NumThreadsZ);

		/* Diffuse the density field */
		// for our purposes this may not be needed Diffusion == 0 for the most part
		for(int i=0;i<10;i++)
		{
			DiffusionConstant_a = dt*FluidParameters.Diffusion*W2*H2;
			DiffusionConstant_c = 1.0f+ 5.0f*DiffusionConstant_a;
			shader.SetFloat("DiffusionConstant_a", DiffusionConstant_a);
			shader.SetFloat("DiffusionConstant_c", DiffusionConstant_c);

			/* ping-pong buffers */
			shader.SetTexture(Kernels.GaussSeidelIteration, "Density_in", density);
			shader.SetTexture(Kernels.GaussSeidelIteration, "Density_out", tex);
			shader.Dispatch(Kernels.GaussSeidelIteration, NumThreadsX,NumThreadsY, NumThreadsZ);
		}

		/* advect the density field using the velocity */
		shader.SetTexture(Kernels.AdvectC, "Density_in", tex);
		shader.SetTexture(Kernels.AdvectC, "Velocity_in", tex2);
		shader.SetTexture(Kernels.AdvectC, "Density_out", density);
		shader.Dispatch(Kernels.AdvectC, NumThreadsX,NumThreadsY,NumThreadsZ);

		/* convert density to colour */
		shader.SetTexture(Kernels.ConvertDensityToColour, "Density_in", density);
		shader.SetTexture(Kernels.ConvertDensityToColour, "Colour_out", tex2);		
		shader.Dispatch(Kernels.ConvertDensityToColour, NumThreadsX,NumThreadsY,NumThreadsZ);
		Graphics.CopyTexture(tex2,density);
		/* set the texture of the object this is attached to */
		rend.material.SetTexture("_MainTex",density);

		/* copy the texture back to the starting point, shouldn't need to do this if we do things properly later */
		//Graphics.CopyTexture(tex2,tex);
	}
	
    // Update is called once per frame
    void Update()
    {
        UpdateShader2();
    }
}
