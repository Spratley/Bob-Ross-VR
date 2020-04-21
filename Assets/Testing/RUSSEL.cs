using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RUSSEL : MonoBehaviour
{
	public ComputeShader shader;

	public RenderTexture theResult;
	public Texture2D theTexture;
	private Renderer rend;

	int kernelRUSSELCS = 0;

    // Start is called before the first frame update
    void Start()
    {

    	theResult = new RenderTexture(256,256,24);
    	theResult.enableRandomWrite = true;
    	theResult.Create();

        /* find the kernel */
        /* set any textures */

    	shader.SetTexture(kernelRUSSELCS, "Result", theResult);
    	shader.SetTexture(kernelRUSSELCS, "inputTexture", theTexture);

    	/* dispatch the shader */
    	shader.Dispatch(kernelRUSSELCS, theResult.width/8, theResult.height/8, 1);
		rend = GetComponent<Renderer>();
		rend.enabled = true;

    	rend.material.SetTexture("_MainTex", theResult);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
