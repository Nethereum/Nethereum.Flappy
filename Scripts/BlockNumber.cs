using System;
using System.Collections;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.UnityClient;
using Nethereum.RPC.Eth.Blocks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class BlockNumber : MonoBehaviour {
    public Text blockNumberText;
    private float blockCheckRate = 3f;
    private float lastBlockCheckTime;
    private bool supportsProperThreads = false;

    void Start () {
        lastBlockCheckTime = 0f;

        if (!supportsProperThreads) {
            StartCoroutine (CheckBlockNumber ());
        }
    }

    public IEnumerator CheckBlockNumber () {
        var wait = 1;
        while (true) {
            yield return new WaitForSeconds (wait);
            wait = 10;
            var blockNumberRequest = new EthBlockNumberUnityRequest ("https://rinkeby.infura.io/v3/7238211010344719ad14a89db874158c");
            yield return blockNumberRequest.SendRequest ();
            if (blockNumberRequest.Exception == null) {
                var blockNumber = blockNumberRequest.Result.Value;
                blockNumberText.text = "Block: " + blockNumber.ToString ();
            }
        }
    }

    public void Update () {
        if (supportsProperThreads) {
            lastBlockCheckTime += Time.deltaTime;

            if (lastBlockCheckTime >= blockCheckRate) {
                lastBlockCheckTime = 0f;
                //CheckBlockNumberOnBackgroundThread();
            }
        }
    }

    /* 
    	public void CheckBlockNumberOnBackgroundThread()
    	{
    		Task<HexBigInteger>.Run(async () => 
    		{
    			return await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();

    		}).ContinueWith( blockNumber => 
    		{
    			blockNumberText.text = "Block: " + blockNumber.Result.Value.ToString();
    		}, 
    		TaskScheduler.FromCurrentSynchronizationContext());
    	}

    	*/

}
