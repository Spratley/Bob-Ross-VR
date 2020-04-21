using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;



public class CS_BAXTER : MonoBehaviour
{
	public ComputeShader shader;
	//public RenderTexture tex,tex2,V,V2,Pressure,density,brush2;
	[SerializeField] public Texture2D initialTexture;
	//[SerializeField] public Texture2D initialVelocityTexture;
	//[SerializeField] public Texture2D BrushTexture;
	[SerializeField] public Texture2D brush,brush2, BRUSH_MASK;

	/* baxter vars */

	public RenderTexture VELOCITY_PAINT, VELOCITY_PAINT_TEMP;
	public RenderTexture DIVERGENCE_PAINT, PRESSURE_PAINT;
	//public RenderTexture brushT,brushT2;
	// for RENDERING ONLY: 
		// base canvas texture reflectance 8 channels
		// painting reflectance (temp) 8 channels
		// painting RGB composite 3 channels, RGBA 
	public RenderTexture PAINT_RGB_COMPOSITE;

		// brush rgb composite (why?) 3 channels, RGBA

	// FOR INTERACTION & RENDERING
		// painting pigment concentrations      			(ADVECTED) (8 channels, 2 RGBA textures) (8 different pigments only)
	public RenderTexture PAINT_PIGMENTS_1,PAINT_PIGMENTS_2;  // WE WILL START WITH JUST 1 OF THEM AND THEN EXPAND TO THE OTHER TO HOLD MORE COLOURS
	public RenderTexture PAINT_PIGMENTS_1_TEMP,PAINT_PIGMENTS_2_TEMP;
	public RenderTexture BASE_LAYER_PIGMENTS_1, BASE_LAYER_PIGMENTS_2;
	
		// painting thickness/paint volume (base/dry/wet)	(ADVECTED) (3 channels, 1 texture) (each channel is a layer)
	public RenderTexture PAINT_VOLUME;

		// brush pigment concentrations						(DEPOSITED/TRANSFERRED) (8 channels, 2 textures)
	public RenderTexture BRUSH_PIGMENTS_1, BRUSH_PIGMENTS_2;  // WE WILL START WITH JUST 1 OF THEM AND THEN EXPAND TO THE OTHER TO HOLD MORE COLOURS

		// brush paint volume								(DEPOSITED/TRANSFERRED) (3 channels, 1 texture)
	public RenderTexture BRUSH_VOLUME;
	public RenderTexture HEIGHT_MAP;
	public RenderTexture NORMAL_MAP;

	public Texture2D initialBrush;


	// FOR INTERACTION ONLY
		// brush penetration on canvas (base/dry/wet) (BRUSH MASK?) don't think we need this due to the way we are doing it... at least for now
		// I bet we could use a mask to set the boundaries for computing 
		// brush velocity (FROM MOUSE)
		// paint undo buffer (OPTIONAL)



	// rendering takes as input the pigment concentraitons and voluem per layer then per layer calc's the K/S parameter mix, Reflectance/transmittance, total reflectance and converts to RGB

	struct BRUSH_BRISTLE{
		public Vector4 pigment1;
		public Vector4 pigment2;
	}


	private Renderer rend;
    public Camera camera;
    bool MOUSE_DOWN = false;
    bool MOUSE_DOWN_PIGMENT = false;
    bool MOUSE_PICKUP = false;
	public Vector3 mousePosition;
	public Vector3 mousePositionPrev;
	public Vector3 mousePositionPICKUP;

	Vector4 channel1,channel2;
  	Color color, color2;
  	Color[] maskPixels;
  	public Vector4 BRUSH_P1;
	float DIR = 0.0f;

	public int w,h, W2,H2;
    int NumThreadsX, NumThreadsY, NumThreadsZ;
    float DiffusionConstant_a;
	float DiffusionConstant_c;
	float dt;
	public float THETA;


	static class Kernels
    {
   		public const int InitCanvas = 0;
   	 	public const int AddBrushVelocity = 1;
        public const int Advect = 2;
        public const int AdvectC = 3;
        public const int GaussSeidelIteration = 4;
		public const int Project1_Divergence = 5;
		public const int Project3_ApplyPressure = 6;
		public const int ConvertPigmentsToRGB = 7;
		public const int AddDensityToLocation = 8;
		public const int TransferBrushPaint = 9;
		public const int DepositBrushPaint = 10;
		public const int ComputeNormalMap = 11;

    }


	[Range(0.0f, 10.0f)]
	public float VISCOSITY = 1.0f;
	[Range(0.0f, 1000.0f)]
	public float DIFFUSION = 1.0f;

	[Range(0.0f, 100.0f)]
	public float BRUSH_VEL_STRENGTH = 25.0f;
	[Range(0.0f, 100.0f)]
	public float INITIAL_BRUSH_PAINT_STRENGTH = 10.0f;
	public float BRUSH_PAINT_STRENGTH=10.0f;
	[Range(0.0f, 1.0f)]
	public float BRUSH_PAINT_DEPOSIT_RATE = 0.99f;
	[Range(1, 80)]
	public int BRUSH_WIDTH = 20;
	[Range(0.0f,0.01f)]
	public float PAINT_DRY_RATE=0.001f;
     class FluidParameters
    {
    	static public float ViscosityInitial =  .000005f;// higher is thicker, smaller is thinner
    	static public float Viscosity =  .000005f;// higher is thicker, smaller is thinner 
    	static public float Diffusion = 0.000000001f;
    	static public float DiffusionInitial = 0.000000001f;

    }

    // Start is called before the first frame update
    void Start()
    {
    	
     	mousePositionPrev 	= new Vector3(0,0,0);
     	mousePosition 		= new Vector3(0,0,0);
     	channel1 			= new Vector4(0,0,0,0);
     	channel2 			= new Vector4(0,0,0,0);
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

    void InitializeRenderTextures(int w, int h)
    {
		VELOCITY_PAINT 		= CreateRenderTexture(w,h);
		VELOCITY_PAINT_TEMP = CreateRenderTexture(w,h);
		DIVERGENCE_PAINT 	= CreateRenderTexture(w,h);
		PRESSURE_PAINT 		= CreateRenderTexture(w,h);
		PAINT_RGB_COMPOSITE = CreateRenderTexture(w,h);

		PAINT_PIGMENTS_1 		= CreateRenderTexture(w,h);
		PAINT_PIGMENTS_2 		= CreateRenderTexture(w,h);
		PAINT_PIGMENTS_1_TEMP 	= CreateRenderTexture(w,h);
		PAINT_PIGMENTS_2_TEMP 	= CreateRenderTexture(w,h);
		BASE_LAYER_PIGMENTS_1 = CreateRenderTexture(w,h);
		BASE_LAYER_PIGMENTS_2 = CreateRenderTexture(w,h);
		
		PAINT_VOLUME 	 = CreateRenderTexture(w,h);
		BRUSH_PIGMENTS_1 = CreateRenderTexture(w,h);
		BRUSH_PIGMENTS_2 = CreateRenderTexture(w,h);
		BRUSH_VOLUME 	 = CreateRenderTexture(w,h);
		HEIGHT_MAP		 = CreateRenderTexture(w,h,1);
		NORMAL_MAP		 = CreateRenderTexture(w,h);
    }


	void InitializeShader()
	{
		dt = 0.01f;
		w  = initialTexture.width; 
		h = initialTexture.height;
		W2 = w/2;
		H2 = h/2;
		NumThreadsX = w/8;	
		NumThreadsY = h/8;	
		NumThreadsZ = 1;
		BRUSH_P1 = new Vector4(0,0,0,0);
	 	rend = GetComponent<Renderer>();
	 	rend.enabled = true;
	 	rend.material.EnableKeyword("_PARALLAXMAP");
	 	rend.material.EnableKeyword("_NORMALMAP");

		rend.material.SetTexture("_HeightMap",initialTexture);

	 	InitializeRenderTextures(w,h);
		Graphics.Blit(initialTexture, PAINT_RGB_COMPOSITE);
	 	brush      = new Texture2D(w,h, TextureFormat.RGBA32,false,true);
	 	brush2     = new Texture2D(w,h, TextureFormat.RGBA32,false,true);
	 	BRUSH_MASK = new Texture2D(w,h, TextureFormat.RGBA32,false,true);
	 	maskPixels = new Color[w*h];

		for (int i = 0; i < maskPixels.Length; i++) maskPixels[i] = Color.black;
		BRUSH_MASK.SetPixels(maskPixels);
		BRUSH_MASK.Apply();

	 	/* initialize all values in canvas */
		shader.SetTexture(Kernels.InitCanvas,"Pigments1_out",PAINT_PIGMENTS_1);
		shader.SetTexture(Kernels.InitCanvas,"Pigments2_out",PAINT_PIGMENTS_2);
		shader.SetTexture(Kernels.InitCanvas,"PaintVolume_out",PAINT_VOLUME);
		shader.SetTexture(Kernels.InitCanvas,"Velocity_out",VELOCITY_PAINT);	
		shader.SetTexture(Kernels.InitCanvas,"HEIGHTMAP_in", initialTexture);

		shader.SetTexture(Kernels.InitCanvas,"BaseLayer1_out", BASE_LAYER_PIGMENTS_1);
		shader.SetTexture(Kernels.InitCanvas,"BaseLayer2_out", BASE_LAYER_PIGMENTS_2);
		shader.SetTexture(Kernels.InitCanvas,"HEIGHTMAP_out", HEIGHT_MAP);
 		shader.Dispatch(Kernels.InitCanvas, NumThreadsX,NumThreadsY,NumThreadsZ);
	}

	void ADVECT(RenderTexture FIELD, RenderTexture FIELD_OUT, RenderTexture VELOCITY_FIELD, RenderTexture HEIGHTMAP_in)
	{
		//for(int i=0;i<5;i++)
		{
			/* advect velocity field */
			shader.SetTexture(Kernels.Advect, "Density_in", FIELD);
			shader.SetTexture(Kernels.Advect, "Velocity_in", VELOCITY_FIELD);
			shader.SetTexture(Kernels.Advect, "Density_out", FIELD_OUT);
			shader.SetTexture(Kernels.Advect, "Boundary_in", initialTexture); // FIXME
			shader.SetTexture(Kernels.Advect,"HEIGHTMAP_in",HEIGHTMAP_in);

			shader.Dispatch(Kernels.Advect, NumThreadsX,NumThreadsY,NumThreadsZ);
			// shader.SetTexture(Kernels.Advect, "Density_in", FIELD_OUT);
			// shader.SetTexture(Kernels.Advect, "Velocity_in", VELOCITY_FIELD);
			// shader.SetTexture(Kernels.Advect, "Density_out", FIELD);
			// shader.Dispatch(Kernels.Advect, NumThreadsX,NumThreadsY,NumThreadsZ);
		}
		//Graphics.CopyTexture(FIELD,FIELD_OUT);
	}

	void DIFFUSE(int N, float _diffusion, RenderTexture Field, RenderTexture temp, RenderTexture HEIGHTMAP_in)
	{
		for(int i=0;i<N;i++)
		{
		 		DiffusionConstant_a = dt*_diffusion*W2*H2;
		 		DiffusionConstant_c = 1.0f+ 5.0f*DiffusionConstant_a;
		 		shader.SetFloat("DiffusionConstant_a", DiffusionConstant_a);
		 		shader.SetFloat("DiffusionConstant_c", DiffusionConstant_c);

		 		/* ping-pong buffers */
		 		shader.SetTexture(Kernels.GaussSeidelIteration, "Density_in", Field);
		 		shader.SetTexture(Kernels.GaussSeidelIteration, "Density_out", temp);
		 		shader.SetTexture(Kernels.GaussSeidelIteration,"HEIGHTMAP_in",HEIGHTMAP_in);
		 		shader.Dispatch(Kernels.GaussSeidelIteration, NumThreadsX,NumThreadsY, NumThreadsZ);
		 		shader.SetTexture(Kernels.GaussSeidelIteration, "Density_in", temp);
		 		shader.SetTexture(Kernels.GaussSeidelIteration, "Density_out", Field);
		 		shader.Dispatch(Kernels.GaussSeidelIteration, NumThreadsX,NumThreadsY, NumThreadsZ);
		}
	}

	void PROJECT(RenderTexture VELOCITY_FIELD_IN, RenderTexture DIVERGENCE_OUT, RenderTexture PRESSURE_OUT)
	{
		shader.SetTexture(Kernels.Project1_Divergence, "Velocity_in", VELOCITY_FIELD_IN);
		shader.SetTexture(Kernels.Project1_Divergence, "Divergence_out", DIVERGENCE_OUT);
		shader.SetTexture(Kernels.Project1_Divergence, "Pressure_out", PRESSURE_OUT);
		shader.Dispatch(Kernels.Project1_Divergence, NumThreadsX,NumThreadsY,NumThreadsZ);

		
	}
	void SOLVE_PRESSURE(int N, RenderTexture DIVERGENCE_IN, RenderTexture PRESSURE_IN, RenderTexture VELOCITY_FIELD_IN, RenderTexture VELOCITY_FIELD_OUT, RenderTexture HEIGHTMAP_in)
	{
	 	/* solve for the pressure */
		for(int i=0;i<N;i++)
		{
			DiffusionConstant_a = 1;
			DiffusionConstant_c = 6;
			shader.SetFloat("DiffusionConstant_a", DiffusionConstant_a);
			shader.SetFloat("DiffusionConstant_c", DiffusionConstant_c);
			shader.SetTexture(Kernels.GaussSeidelIteration,"HEIGHTMAP_in",HEIGHTMAP_in);
			shader.SetTexture(Kernels.GaussSeidelIteration, "Density_in", DIVERGENCE_IN); // input divergence
			shader.SetTexture(Kernels.GaussSeidelIteration, "Density_out", PRESSURE_IN); //output pressure
			shader.Dispatch(Kernels.GaussSeidelIteration, NumThreadsX,NumThreadsY, NumThreadsZ);
		}

		/* now finish the pressure projection */
		shader.SetTexture(Kernels.Project3_ApplyPressure, "Pressure_in", PRESSURE_IN);
		shader.SetTexture(Kernels.Project3_ApplyPressure, "Velocity_in", VELOCITY_FIELD_IN);
		shader.SetTexture(Kernels.Project3_ApplyPressure, "Velocity_out", VELOCITY_FIELD_OUT);
		shader.Dispatch(Kernels.Project3_ApplyPressure, NumThreadsX,NumThreadsY, NumThreadsZ);
	}

	void CONVERT_PIGMENTS_TO_RGB(RenderTexture PIGMENTS1, RenderTexture PIGMENTS2, 
								 RenderTexture VOLUME, RenderTexture RGB_OUT, RenderTexture HEIGHT_OUT){

		shader.SetTexture(Kernels.ConvertPigmentsToRGB,"Pigments1_in",PIGMENTS1);
		shader.SetTexture(Kernels.ConvertPigmentsToRGB,"Pigments2_in",PIGMENTS2);
		shader.SetTexture(Kernels.ConvertPigmentsToRGB,"Pigments1_out",PIGMENTS1);
		shader.SetTexture(Kernels.ConvertPigmentsToRGB,"Pigments2_out",PIGMENTS2);
		shader.SetTexture(Kernels.ConvertPigmentsToRGB,"PaintVolume_in",VOLUME);
		shader.SetTexture(Kernels.ConvertPigmentsToRGB,"Colour_out",RGB_OUT);
		shader.SetTexture(Kernels.ConvertPigmentsToRGB,"HEIGHTMAP_out",HEIGHT_OUT);
		shader.SetTexture(Kernels.ConvertPigmentsToRGB,"BaseLayer1_in", BASE_LAYER_PIGMENTS_1);
		shader.SetTexture(Kernels.ConvertPigmentsToRGB,"BaseLayer2_in", BASE_LAYER_PIGMENTS_2);
		shader.SetTexture(Kernels.ConvertPigmentsToRGB,"BaseLayer1_out", BASE_LAYER_PIGMENTS_1);
		shader.SetTexture(Kernels.ConvertPigmentsToRGB,"BaseLayer2_out", BASE_LAYER_PIGMENTS_2);
		shader.SetTexture(Kernels.ConvertPigmentsToRGB, "CANVAS_in", initialTexture);

		shader.Dispatch(Kernels.ConvertPigmentsToRGB, NumThreadsX,NumThreadsY, NumThreadsZ);
	}
	void COMPUTE_NORMAL_MAP(RenderTexture PAINT_RGB_COMPOSITE_IN, RenderTexture HEIGHT_MAP_IN, RenderTexture NORMAL_MAP_OUT){
		shader.SetTexture(Kernels.ComputeNormalMap, "Density_in", PAINT_RGB_COMPOSITE_IN);
		shader.SetTexture(Kernels.ComputeNormalMap, "HEIGHTMAP_in", HEIGHT_MAP_IN);
		shader.SetTexture(Kernels.ComputeNormalMap, "CANVAS_in", initialTexture);
		shader.SetTexture(Kernels.ComputeNormalMap, "Normals_out", NORMAL_MAP_OUT);
		shader.Dispatch(Kernels.ComputeNormalMap, NumThreadsX,NumThreadsY, NumThreadsZ);
	}

	void SetColors(){

		if(Input.GetKeyDown("1")){ channel1.Set(1,0,0,0); channel2.Set(0,0,0,0);}
		if(Input.GetKeyDown("2")){ channel1.Set(0,1,0,0); channel2.Set(0,0,0,0);}
		if(Input.GetKeyDown("3")){ channel1.Set(0,0,1,0); channel2.Set(0,0,0,0);}
		if(Input.GetKeyDown("4")){ channel1.Set(0,0,0,1); channel2.Set(0,0,0,0);}
		if(Input.GetKeyDown("5")){ channel1.Set(0,0,0,0); channel2.Set(1,0,0,0);}
		if(Input.GetKeyDown("6")){ channel1.Set(0,0,0,0); channel2.Set(0,1,0,0);}
		if(Input.GetKeyDown("7")){ channel1.Set(0,0,0,0); channel2.Set(0,0,1,0);}
		if(Input.GetKeyDown("8")){ channel1.Set(0,0,0,0); channel2.Set(0,0,0,1);}
		if(Input.GetKeyDown("0")){ channel1.Set(0,0,0,0); channel2.Set(0,0,0,0);}
	}
	void UpdateShaderBAXTER(){
		shader.SetFloat("theTime", Time.time);
		shader.SetFloat("dt", dt);
		shader.SetFloat("PAINT_DRY_RATE",PAINT_DRY_RATE);
	
		SetColors();
		for (int i = 0; i < maskPixels.Length; i++) maskPixels[i] = Color.black;
		BRUSH_MASK.SetPixels(maskPixels);
		BRUSH_MASK.Apply();
		/* HANDLE MOUSE INTERACTION / BRUSH HERE */
		RaycastHit hit;
		Ray ray = camera.ScreenPointToRay(Input.mousePosition);

		FluidParameters.Viscosity = FluidParameters.ViscosityInitial*VISCOSITY;
		FluidParameters.Diffusion = FluidParameters.DiffusionInitial*DIFFUSION;
		mousePositionPrev = mousePosition;
		if (Physics.Raycast(ray, out hit)) {
				float IDX,IDY;
				mousePosition 	  = hit.textureCoord;
				IDX = hit.textureCoord.x - mousePositionPrev.x;
				IDY = hit.textureCoord.y - mousePositionPrev.y;
				
				if(Input.GetMouseButtonDown(0)) MOUSE_DOWN = true;
				if(Input.GetMouseButtonUp(0)  ) 
				{
					MOUSE_DOWN = false;
					BRUSH_PAINT_STRENGTH = INITIAL_BRUSH_PAINT_STRENGTH;
					//BRUSH_MASK  = new Texture2D(w,h, TextureFormat.RGBA32,false,true);
					
				}

				if(Input.GetMouseButtonDown(2)  ) MOUSE_PICKUP = true;
				if(Input.GetMouseButtonUp(2)  ) MOUSE_PICKUP = false;
				if(Input.GetMouseButtonDown(1)) MOUSE_DOWN_PIGMENT = true;
				if(Input.GetMouseButtonUp(1)  ) 
				{
					MOUSE_DOWN_PIGMENT = false;
					brush 		= new Texture2D(w,h, TextureFormat.RGBA32,false,true);
					brush2  	= new Texture2D(w,h, TextureFormat.RGBA32,false,true);
					//BRUSH_MASK  = new Texture2D(w,h, TextureFormat.RGBA32,false,true);
					// for (int i = 0; i < maskPixels.Length; i++) maskPixels[i] = Color.black;
					// BRUSH_MASK.SetPixels(maskPixels);
					// BRUSH_MASK.Apply();
				}
				//mousePositionPrev = mousePosition;
				
				float alpha = 0.8f;
				//if(Mathf.Abs(IDX) < 0.0004) IDX = 0.0f;
				//if(Mathf.Abs(IDY) < 0.0004) IDY = 0.0f;
			
				
				//THETA = Mathf.Atan2(IDY,IDX);
				if(IDX < 0.0f) DIR = -1.0f;
				if(IDX > 0.0f) DIR = 1.0f;
				float DEG2RAD = Mathf.PI/180.0f;
				float RAD2DEG = 180.0f/Mathf.PI;
			//	if(THETA == 0.0f && DIR == -1) THETA = -Mathf.PI;
				//float angle=(Mathf.Abs(Mathf.Atan2(IDY,IDX)));// + 360.0f)%360.0f; // 0.. 2pi
				float angle=0.0f;//
				angle =((Mathf.Atan2(IDY,IDX)));
				if(angle < 0) angle = Mathf.PI *2.0f - (-angle);
				//if(angle > 2.0f*Mathf.PI) angle = angle - Mathf.PI*2.0f;


				//if(DIR == -1 && angle == 0.0f) angle += Mathf.PI;
				//float angle = 0.0f;
			

				// if(IDX > 0 && IDY > 0){
				// 	// Q1
				// 	angle = Mathf.Abs(Mathf.Atan2(IDY,IDX));
				// }
				// if(IDX < 0 && IDY > 0){
				// 	// Q2
				// 	angle = Mathf.Abs(Mathf.Atan2(IDY,IDX));
				// }				

				// if(IDX < 0 && IDY < 0){
				// 	// Q3
				// 	angle = Mathf.PI+Mathf.Abs(Mathf.Atan2(-IDY,-IDX));
				// }
				// if(IDX > 0 && IDY < 0){
				// 	// Q4
				// 	angle = Mathf.PI + Mathf.Abs(Mathf.Atan2(-IDY,-IDX));//Mathf.Abs(Mathf.Atan2(-IDY,IDX));
				// }



				//if(DIR == 1) 
				// /angle = Mathf.Atan2(IDY,IDX);
				//if(DIR == -1) angle = Mathf.PI+Mathf.Atan2(-IDY,-IDX);
				//if(IDX == 0.0f) angle = (DIR<0)? -Mathf.PI/2.0f: Mathf.PI/2.0f;
				//angle += Mathf.Ceil(-angle/360.0f)*360.0f;
				//if(IDX < 0) angle += 2.0f*Mathf.PI;
				//if(THETA == Mathf.PI || THETA == -Mathf.PI) THETA = 0.0f;
				if(IDX != 0.0f && IDY != 0.0f) THETA = angle;//alpha*THETA + (1.0f-alpha)*(angle);
				//if(IDX == 0 ) THETA = -DIR*Mathf.PI;
				//while(THETA > Mathf.PI) THETA -= Mathf.PI;
				//while(THETA < -Mathf.PI) THETA += Mathf.PI;
				Debug.Log("IDX:"+IDX+" IDY:"+IDY+" Theta="+angle*RAD2DEG);

				int bw = BRUSH_WIDTH;
				float bwf = (float)(bw/2)/(float)brush.width;
				/* setup global shader values for input */
				shader.SetFloat("InputX", hit.textureCoord.x);
				shader.SetFloat("InputY", hit.textureCoord.y);
				shader.SetFloat("IDX", IDX);
				shader.SetFloat("IDY", IDY);
				shader.SetFloat("BRUSH_WIN_WIDTH", bwf);
				shader.SetVector("BrushPaint1", channel1);
				shader.SetVector("BrushPaint2", channel2);
				
				//if(MOUSE_DOWN || MOUSE_DOWN_PIGMENT)
				{
					for(int i=-bw;i<bw;i++)
					{
						for(int j=-bw;j<bw;j++)
						{
							float width = brush.width;
							float height = brush.height;
							float bx = (float)(hit.textureCoord.x*width);
							float by = (float) (hit.textureCoord.y*height);
							float xR = (float)i;
							float yR = (float)j;
							// xR,yR are the coordinates in brush-window coords, 
							// ox,oy are the pixel coordinates of where we are drawing the current brush pixel
							float cTheta = Mathf.Cos(THETA);
							float sTheta = Mathf.Sin(THETA);
							float xprime,yprime;
							float xpix = xR;
							float ypix = yR;
							xprime = xpix * cTheta - ypix*sTheta;
							yprime = xpix * sTheta + ypix*cTheta;
							int ox = (int)(bx + xprime);//bx + xR);
							int oy = (int)(by + yprime);//by + yR);

							// xx,yy are the coordinates in the brush image
							int xx = (int)(((float)(i)/((float)bw*2)+0.5f)*initialBrush.width);
							int yy = (int)(((float)(j)/((float)bw*2)+0.5f)*initialBrush.height);
							
							float bval = initialBrush.GetPixel(xx,yy).grayscale * BRUSH_PAINT_STRENGTH;

							BRUSH_MASK.SetPixel(ox, oy,new Color(bval,bval,bval,bval));
						}
					}
				}
				BRUSH_MASK.Apply();

            	if(MOUSE_DOWN_PIGMENT)
            	{
            		/* need to deposit paint */
            		/* need to pick up paint from canvas */
					shader.SetTexture(Kernels.AddDensityToLocation, "Density_in", PAINT_PIGMENTS_1);
					shader.SetTexture(Kernels.AddDensityToLocation, "Density_out", PAINT_PIGMENTS_1);
					shader.SetTexture(Kernels.AddDensityToLocation, "Density2_in", PAINT_PIGMENTS_2);
					shader.SetTexture(Kernels.AddDensityToLocation, "Density2_out", PAINT_PIGMENTS_2);
					shader.SetTexture(Kernels.AddDensityToLocation, "BrushMask_in", BRUSH_MASK);

					
					shader.Dispatch(Kernels.AddDensityToLocation, NumThreadsX,NumThreadsY, NumThreadsZ);
					// Graphics.CopyTexture(brush,BRUSH_PIGMENTS_1);
					// Graphics.CopyTexture(brush2,BRUSH_PIGMENTS_2);

					// shader.SetTexture(Kernels.DepositBrushPaint, "BrushMask_in", BRUSH_MASK);
					// shader.SetTexture(Kernels.DepositBrushPaint, "BrushTexture_in", BRUSH_PIGMENTS_1);
					// shader.SetTexture(Kernels.DepositBrushPaint, "BrushTexture_out", BRUSH_PIGMENTS_1);
					// shader.SetTexture(Kernels.DepositBrushPaint, "BrushTexture2_in", BRUSH_PIGMENTS_2);
					// shader.SetTexture(Kernels.DepositBrushPaint, "BrushTexture2_out", BRUSH_PIGMENTS_2);
					// shader.SetTexture(Kernels.DepositBrushPaint, "Density_out", PAINT_PIGMENTS_1);
					// shader.SetTexture(Kernels.DepositBrushPaint, "Density2_out", PAINT_PIGMENTS_2);
					// shader.SetTexture(Kernels.DepositBrushPaint,"BaseLayer1_in", BASE_LAYER_PIGMENTS_1);
					// shader.SetTexture(Kernels.DepositBrushPaint,"BaseLayer2_in", BASE_LAYER_PIGMENTS_2);
					// shader.SetTexture(Kernels.DepositBrushPaint,"BaseLayer1_out", BASE_LAYER_PIGMENTS_1);
					// shader.SetTexture(Kernels.DepositBrushPaint,"BaseLayer2_out", BASE_LAYER_PIGMENTS_2);
					// shader.SetTexture(Kernels.DepositBrushPaint,"HEIGHTMAP_in", HEIGHT_MAP);
					
					// shader.Dispatch(Kernels.DepositBrushPaint, NumThreadsX,NumThreadsY, NumThreadsZ);				

				}

				if(MOUSE_PICKUP)
				{
						Debug.Log("pickup");
						mousePositionPICKUP.x = mousePositionPrev.x;
						mousePositionPICKUP.y = mousePositionPrev.y;
						shader.SetFloat("InputX", mousePositionPICKUP.x);
						shader.SetFloat("InputY", mousePositionPICKUP.y);
						shader.SetTexture(Kernels.TransferBrushPaint, "BrushTexture_in", PAINT_PIGMENTS_1);
						shader.SetTexture(Kernels.TransferBrushPaint, "BrushTexture_out", BRUSH_PIGMENTS_1);
						shader.SetTexture(Kernels.TransferBrushPaint, "BrushTexture2_in", PAINT_PIGMENTS_2);
						shader.SetTexture(Kernels.TransferBrushPaint, "BrushTexture2_out", BRUSH_PIGMENTS_2);

	 					shader.SetTexture(Kernels.TransferBrushPaint, "Density_out", PAINT_PIGMENTS_1);
						shader.SetTexture(Kernels.TransferBrushPaint, "Density2_out", PAINT_PIGMENTS_2);
						shader.SetTexture(Kernels.TransferBrushPaint, "BrushMask_in", BRUSH_MASK);

						shader.Dispatch(Kernels.TransferBrushPaint, NumThreadsX,NumThreadsY, NumThreadsZ);
				}
				if(MOUSE_DOWN || MOUSE_DOWN_PIGMENT)
				{


						shader.SetFloat("InputX", hit.textureCoord.x);
						shader.SetFloat("InputY", hit.textureCoord.y);

						 //  AsyncGPUReadbackRequest request;

						 // request = AsyncGPUReadback.Request(PAINT_PIGMENTS_1,0, (int)mousePosition.x, 1, (int)mousePosition.y, 1, 0, 0, TextureFormat.RGBA32,null);
						 // var buffer = request.GetData<float>();
						 // bristleBuffer.GetData(bristleDataOUT);
						 // Debug.Log("bristleData = " + bristleDataOUT[0].pigment1);

						 //BRUSH_PIGMENTS_1.ReadPixels(new Rect(mousePosition.x, mousePosition.y, 1,1),0,0);

						

						 // shader.SetFloat("PickX", mousePositionPICKUP.x);
						 // shader.SetFloat("PickY", mousePositionPICKUP.y);

						// shader.SetFloat("InputX", mousePosition.x);
						// shader.SetFloat("InputY", mousePosition.y);
						// shader.SetTexture(Kernels.TransferBrushPaint, "BrushTexture_in", PAINT_PIGMENTS_1);
						// shader.SetTexture(Kernels.TransferBrushPaint, "BrushTexture_out", BRUSH_PIGMENTS_1);
						// shader.SetTexture(Kernels.TransferBrushPaint, "BrushTexture2_in", PAINT_PIGMENTS_2);
						// shader.SetTexture(Kernels.TransferBrushPaint, "BrushTexture2_out", BRUSH_PIGMENTS_2);
	 				// 	shader.SetTexture(Kernels.TransferBrushPaint, "Density_out", PAINT_PIGMENTS_1);
						// shader.SetTexture(Kernels.TransferBrushPaint, "Density2_out", PAINT_PIGMENTS_2);
						// shader.SetTexture(Kernels.TransferBrushPaint, "BrushMask_in", BRUSH_MASK);
						// shader.Dispatch(Kernels.TransferBrushPaint, NumThreadsX,NumThreadsY, NumThreadsZ);


						
						shader.SetTexture(Kernels.DepositBrushPaint, "BrushMask_in", BRUSH_MASK);
						shader.SetTexture(Kernels.DepositBrushPaint, "BrushTexture_in", BRUSH_PIGMENTS_1);
						shader.SetTexture(Kernels.DepositBrushPaint, "BrushTexture_out", BRUSH_PIGMENTS_1);
						shader.SetTexture(Kernels.DepositBrushPaint, "BrushTexture2_in", BRUSH_PIGMENTS_2);
						shader.SetTexture(Kernels.DepositBrushPaint, "BrushTexture2_out", BRUSH_PIGMENTS_2);
						shader.SetTexture(Kernels.DepositBrushPaint, "Density_out", PAINT_PIGMENTS_1);
						shader.SetTexture(Kernels.DepositBrushPaint, "Density2_out", PAINT_PIGMENTS_2);
						shader.SetTexture(Kernels.DepositBrushPaint,"BaseLayer1_in", BASE_LAYER_PIGMENTS_1);
						shader.SetTexture(Kernels.DepositBrushPaint,"BaseLayer2_in", BASE_LAYER_PIGMENTS_2);
						shader.SetTexture(Kernels.DepositBrushPaint,"BaseLayer1_out", BASE_LAYER_PIGMENTS_1);
						shader.SetTexture(Kernels.DepositBrushPaint,"BaseLayer2_out", BASE_LAYER_PIGMENTS_2);
						shader.SetTexture(Kernels.DepositBrushPaint,"HEIGHTMAP_in", HEIGHT_MAP);
						
						shader.Dispatch(Kernels.DepositBrushPaint, NumThreadsX,NumThreadsY, NumThreadsZ);

						// 
						shader.SetFloat("Strength",BRUSH_VEL_STRENGTH);
						shader.SetTexture(Kernels.AddBrushVelocity,"HEIGHTMAP_in", HEIGHT_MAP);
						shader.SetTexture(Kernels.AddBrushVelocity, "BrushMask_in", BRUSH_MASK);
						shader.SetTexture(Kernels.AddBrushVelocity,"Velocity_in",VELOCITY_PAINT);
						shader.SetTexture(Kernels.AddBrushVelocity,"Velocity_out",VELOCITY_PAINT_TEMP);
				 		shader.Dispatch(Kernels.AddBrushVelocity, NumThreadsX,NumThreadsY, NumThreadsZ);
            			Graphics.CopyTexture(VELOCITY_PAINT_TEMP,VELOCITY_PAINT);

            			BRUSH_PAINT_STRENGTH *= BRUSH_PAINT_DEPOSIT_RATE;
            			Debug.Log("paint strength = " + BRUSH_PAINT_STRENGTH);
            	}
        

        }

        /* AT THIS POINT, ASSUME THAT WE HAVE.... */

        /* NOW HANDLE THE PAINT MOVEMENT */
        /* DIFFUSE/ADVECT THE PAINT VOLUMES AND PIGMENT CONCENTRATIONS */
         DIFFUSE(25, FluidParameters.Viscosity,VELOCITY_PAINT, VELOCITY_PAINT_TEMP, HEIGHT_MAP);

         /* advect velocity field using viscosity */
         ADVECT(VELOCITY_PAINT_TEMP, VELOCITY_PAINT, VELOCITY_PAINT_TEMP, HEIGHT_MAP);

         /* solve for pressure */
         PROJECT(VELOCITY_PAINT, DIVERGENCE_PAINT, PRESSURE_PAINT);

		 SOLVE_PRESSURE(10, DIVERGENCE_PAINT, PRESSURE_PAINT, VELOCITY_PAINT, VELOCITY_PAINT_TEMP, HEIGHT_MAP);

         /* diffuse the pigment concentrations */
		 DIFFUSE(1, FluidParameters.Diffusion, PAINT_PIGMENTS_1, PAINT_PIGMENTS_1_TEMP,HEIGHT_MAP);
		 DIFFUSE(1, FluidParameters.Diffusion, PAINT_PIGMENTS_2, PAINT_PIGMENTS_2_TEMP,HEIGHT_MAP);
		
         /* advect the pigment concentrations with the velocity field */
		 ADVECT(PAINT_PIGMENTS_1_TEMP, PAINT_PIGMENTS_1, VELOCITY_PAINT_TEMP, HEIGHT_MAP);
		 ADVECT(PAINT_PIGMENTS_2_TEMP, PAINT_PIGMENTS_2, VELOCITY_PAINT_TEMP, HEIGHT_MAP);

       // convert to RGB
		CONVERT_PIGMENTS_TO_RGB(PAINT_PIGMENTS_1, PAINT_PIGMENTS_2, PAINT_VOLUME, PAINT_RGB_COMPOSITE, HEIGHT_MAP);
		if(Input.GetKeyDown("s")){ SaveTextureAsPNG( PAINT_RGB_COMPOSITE, "saved.png");}
		COMPUTE_NORMAL_MAP(PAINT_RGB_COMPOSITE, HEIGHT_MAP, NORMAL_MAP);
	 	//rend.material.SetTexture("_ParallaxMap",HEIGHT_MAP);
	 	rend.material.SetTexture("_BumpMap",NORMAL_MAP);
	 	rend.material.SetTexture("_MainTex",PAINT_RGB_COMPOSITE);
	 	rend.material.SetTexture("_HeightMap",NORMAL_MAP);
	 	/* cleanup for next frame */
	 	Graphics.CopyTexture(VELOCITY_PAINT_TEMP,VELOCITY_PAINT);

	 	
	
	}

	
    // Update is called once per frame
    void Update()
    {
        UpdateShaderBAXTER();
    }

    void OnGUI() {
  //	if (orgBoxPos != Vector2.zero && endBoxPos != Vector2.zero) {
    	// Debug.Log("ONGUI");
    	// Vector3 mp = Input.mousePosition;
    	// float bw = BRUSH_WIDTH*4;
    	// float cw = bw/2.0f;
    	//  GUI.DrawTexture(new Rect(mp.x-cw, (Screen.height -mp.y)-cw, bw, bw), initialBrush); // -
  	//}
 	}

    // FIXME : NOT WORKING for rgb texture for some reason.... works for pigments but not actual final composite
    public static void SaveTextureAsPNG( RenderTexture _Rtexture, string _fullPath)
         {
         	
         	Texture2D _texture = new Texture2D(1024, 512, TextureFormat.RGB24, false);
         	RenderTexture.active = _Rtexture;
 			_texture.ReadPixels(new Rect(0, 0, _Rtexture.width, _Rtexture.height), 0, 0);
 			_texture.Apply();

             byte[] _bytes =_texture.EncodeToPNG();
             System.IO.File.WriteAllBytes(_fullPath, _bytes);
             Debug.Log(_bytes.Length/1024  + "Kb was saved as: " + _fullPath + " w:"+_Rtexture.width + " h:"+_Rtexture.height);
         }

}
