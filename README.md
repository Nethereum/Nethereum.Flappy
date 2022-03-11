# Nethereum.Flappy

This is the csharp source code for the Flappy Bird example on how to integrate Unity3d and Ethereum. It is assumed some familiarity with Nethereum as the Unity3d support extends the current implementation.

If you are not familiar with Unity3d, this sample is based on the Unity3d tutorial on how to build a flappy bird style game, full tutorial here: https://unity3d.com/learn/tutorials/topics/2d-game-creation/project-goals

And full game here: https://assetstore.unity.com/packages/templates/flappy-bird-style-example-game-80330

Note: For a simpler get started example and upto date (latest version of Nethereum) of integrating Unity and Nethereum check also this tutorial. https://github.com/Nethereum/Unity3dSimpleSampleNet461

## Working with Unity3d

To enable cross platform compatibility and the threading mechanism using coroutines for Unity3d, Nethereum uses a new type of RPC Client, the UnityRpcClient.

All the Unity RPCRequests inherit now from this client, for example to retrieve the current blocknumber you will need to use:
EthBlockNumberUnityRequest

The UnityRpcClient similarly to other RPC providers accepts an RPCRequest, but internally uses UnityWebRequest which is compatible with Webgl and Il2Cpp.

Coroutines do not support any return values, to overcome this, the client inherits from UnityRequest, which provide the following properties:

```csharp
public class UnityRequest<TResult> {
   public TResult Result { get; set; }
   public Exception Exception { get; set; }
}
```

All this UnityRPC requests, internally wrap the core RPC requests, decoupling the RPC clients but maintaining the integrity with the core requests.

## Simple RPC calls

So how does this work? Let’s start with simple RPC calls, like the one to retrieve the current block number on the Ethereum Flappy Unicorn game.

Note: in this sample, a special INFURA API key is used: `7238211010344719ad14a89db874158c`. If you wish to use this sample in your own project you’ll need to [sign up on INFURA](https://infura.io/register) and use your own key.

```csharp
var blockNumberRequest = new EthBlockNumberUnityRequest("https://rinkeby.infura.io/v3/7238211010344719ad14a89db874158c");
```

Normally we would use web3 to manage all the RPC requests, but for Unity3d we use a specific request per each type of RPC call, including information about the RPC client. 

In this scenario the request is using as the RPC provider the Rinkbey public node provided by Infura.

When requesting a block number we don’t need any parameters, so we can simply just send the request and “yield” until it is complete.

```csharp
yield return blockNumberRequest.SendRequest();
```

Once is completed, we can check for any errors and parse the result

```
if(blockNumberRequest.Exception == null) {
   var blockNumber = blockNumberRequest.Result.Value;
   blockNumberText.text = "Block: " + blockNumber.ToString();
}
```

Full source code:
```csharp
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

    void Start () {
        lastBlockCheckTime = 0f;
        StartCoroutine (CheckBlockNumber ());
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
}
```


## Contract calls

Contract calls are made in a similar way to any other Unity RPC call, but they need the request to be encoded.

For example to retrieve the user top score we will create first a EthCallUnityRequest, responsible to make an eth_call rpc request.

```csharp
var userTopScoreRequest = new EthCallUnityRequest("https://rinkeby.infura.io/v3/7238211010344719ad14a89db874158c");
```
Using the contract ABI and contract address we can then create a new contract and retrieve the function.

```csharp
var contract = new Contract(null, ABI, contractAddress);
var function = new contract.GetFunction("userTopScores");
```

**Note** The contract does not have now the generic RPC client as a constructor parameter.

The contract function can build the call input to retrieve the user top score.

```csharp
var callInput = function.CreateCallInput(userAddress);
var blockParameter = Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest();
yield return userTopScoreRequest.SendRequest(callInput, blockParameter);
```

Once we have retrieved the result successfully we use the function to decode the output.

```csharp
var topScore = function.DecodeSimpleTypeOutput<int>(userTopScoreRequest.Result);
```

Full source code:

```csharp
 //Check if the user top score has changed on the contract chain every 2 seconds
    public IEnumerator CheckTopScore () {
        var wait = 0;
        while (true) {
            yield return new WaitForSeconds (wait);
            wait = 2;

            //Create a unity call request (we have a request for each type of rpc operation)
            var userTopScoreRequest = new EthCallUnityRequest (_url);

            if (_userAddress != null) {

                //Use the service to create a call input which includes the encoded  
                var userTopScoreCallInput = _scoreContractService.CreateUserTopScoreCallInput (_userAddress);
                //Call request sends and yield for response	
                yield return userTopScoreRequest.SendRequest (userTopScoreCallInput, Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest ());

                //Each request has a exception and a result. The exception is set when an error occurs.
                //Follows a similar patter to the www and unitywebrequest

                if (userTopScoreRequest.Exception == null) {
                    //decode the top score using the service
                    var topScoreUser = _scoreContractService.DecodeUserTopScoreOutput (userTopScoreRequest.Result);
                    //and set it to the text box
                    topScoreText.text = "Your top: " + topScoreUser.ToString ();
                    //set the value on the global worl
                    GameControl.instance.TopScoreRecorded = topScoreUser;
                    wait = 3;
                } else {
                    Debug.Log (userTopScoreRequest.Exception.ToString ());
                }

            }
        }
    }

```

## Submitting Transactions
Before we submit the transaction in a similar way when we make a call we need to build the transaction input. For example to submit the user top score.

```csharp
function.CreateTransactionInput(addressFrom, gas, valueAmount, score, v, r, s);
```

In this scenario the input parameters are the score, and the signature values for v, r, s.


### Signing the transactions
There are many way that you can sign the transactions, as usual this depends on where the user stores their private keys and / or what is the best user experience.

An option, could be, that for Desktop and Mobile applications the user can open the web3 secret storage definition file (account file in geth / parity) and sign the transaction with their private key.

```csharp
var transactionSignedRequest = new TransactionSignedUnityRequest(_url, key, _userAddress);
yield return transactionSignedRequest.SignAndSendTransaction(transactionInput);
```


### WebGL and Metamask
Decrypting using WebGL / JavaScript the account file is extremely slow, so when deploying a game to the browser, as per the Unicorn Flappy sample, it makes more sense to delegate the signing to Metamask.

To achieve this you can create your own external library to interact with the injected web3 library in the browser. For more information on External libraries check the Unity documentation https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html

**NOTE**
**Check for an updated version on metamask integration https://github.com/Nethereum/Nethereum.Unity.Webgl**

```csharp
[DllImport("__Internal")]
private static extern string SendTransaction(string to, string data);
```

## Full Source code sample

```csharp

            //Game control sets a signal
            if (GameControl.instance.SubmitTopScore && !submitting) {
                if (_userAddress != null) {
                    submitting = true;
                    Debug.Log ("Submitting tx");

                    //Create the transaction input with encoded values for the function
                    var transactionInput = _scoreContractService.CreateSetTopScoreTransactionInput (_userAddress, _addressOwner, _privateKey,
                        GameControl.instance.TopScore,
                        new HexBigInteger (4712388));

                    if (ExternalProvider) {
                        Debug.Log ("Submitting tx to score using external: " + transactionInput.Data);
                        SendTransaction (transactionInput.To, transactionInput.Data);
                    } else {
                        //Create Unity Request with the private key, url and user address 
                        //(the address could be recovered from private key as in normal Nethereum, could put this an overload)
                        // premature optimisation
                        var transactionSignedRequest = new TransactionSignedUnityRequest (_url, GameControl.instance.Key, _userAddress);

                        //send and wait
                        yield return transactionSignedRequest.SignAndSendTransaction (transactionInput);

                        if (transactionSignedRequest.Exception == null) {
                            //get transaction receipt
                            Debug.Log ("Top score submitted tx: " + transactionSignedRequest.Result);
                        } else {
                            Debug.Log ("Error submitted tx: " + transactionSignedRequest.Exception.Message);
                        }

                    }

                }
                GameControl.instance.SubmitTopScore = false;
                submitting = false;
            }
        }

```

### Javascript Interop
The Javascript interop it is achieved using the JsLib (as per Unity3d docs https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html). This is an example of usage.

**NOTE**
**Check for an updated version on usage and integrated components https://github.com/Nethereum/Nethereum.Unity.Webgl**

```javascript
//This file allows you to interop with Web3js / Metamask include it in your assets folder

mergeInto(LibraryManager.library, {
  GetAccount: function () {
    var account = '';
    if (typeof web3 !== 'undefined') {
        account = web3.eth.accounts[0];
        if(typeof account === 'undefined'){
            account = '';
        }
    }
    var buffer = _malloc(lengthBytesUTF8(account) + 1);
    stringToUTF8(account, buffer, account.length + 1);
    return buffer;
  },

  SendTransaction: function (to, data) {
    var tostr = Pointer_stringify(to);
    var from = web3.eth.accounts[0];
    var datastr = Pointer_stringify(data);
	
	var params = [{
		"from": from,
		"to": tostr,
		"data": datastr
	}];
	var message = {
	  method: 'eth_sendTransaction',
	  params: params,
	  from: from
	};
   
	new Promise(function (resolve, reject) {
            ethereum.send(message, function (error, result) {
                console.log(result);
                resolve(JSON.stringify(result));
         	});
	 }).then(function(response){console.log(response);});
  },
});

```

And the source code of the html file which uses the usual web3js and metamask checks

```html
<!DOCTYPE html>
    <script>
        var gameInstance = UnityLoader.instantiate("gameContainer", "Build/webglmeta4.json", {onProgress: UnityProgress});
        window.addEventListener('load', function() {
        // Checking metamask
        checkMetamask();
    });
	
	function checkMetamask(){
		if (typeof window.ethereum !== 'undefined' && window.ethereum.isMetaMask && window.ethereum.isConnected()) {
		  web3.setProvider(window.ethereum);
          web3.version.getNetwork((err, netId) => {
              if(netId != 4){
                document.getElementById("metamaskWarning").innerText = 'Please connect to Rinkeby to view and submit your top scores';
				document.getElementById("btnConnectToMetamask").style.visibility = "visible";
                web3 = undefined;  
              }else{
                window.web3 = new Web3(web3.currentProvider);
                if(typeof(window.web3.eth.accounts[0]) == 'undefined'){
                  document.getElementById("metamaskWarning").innerText = 'Please unlock Metamask to view and submit your top scores';       
				  document.getElementById("btnConnectToMetamask").style.visibility = "visible";
                }else{
                  document.getElementById("metamaskWarning").innerText = ''; 
				  document.getElementById("btnConnectToMetamask").style.visibility = "hidden";
                }
              }
            });
        } else {
			document.getElementById("metamaskWarning").innerText = 'Please install Metamask and connect to Rinkeby to view and submit your top scores';
        }
	}
	async function connectToMetamask(){
		try {
			await window.ethereum.enable();
			checkMetamask();
		} catch (error) {
		  // Handle error. Likely the user rejected the login
		  console.error(error)
		}
	}
    </script>
  </head>
  <body>
  <div class="container">
    <div class="row">
    <div class="webgl-content">
      <div id="gameContainer" style="width: 960px; height: 600px"></div>
	   
	   <section class="jumbotron text-center">
        <div class="container">
          <h1 class="jumbotron-heading">Ethereum Flappy Unicorn PoC using Nethereum, Metamask and Infura</h1>
          <p class="lead text-muted">Can you beat your top score and the top 5? Your top score will be stored in an ethereum blockchain smart contract once the game is finished</p>
		  <p id="metamaskWarning" class="lead" style="color:red">Please install Metamask and connect to Rinkeby to submit your top score</p>
          <p>
            <a href="#" id="btnConnectToMetamask"  onClick="connectToMetamask()" class="btn btn-secondary btn-lg btn-block">Connect to Metamask</a>
          </p>
        </div>
      </section>



```


### Compiling to Webgl and IOS
When compiling to Webgl or IOS, you need to ensure that dlls are not stripped when running IL2CPP. An example of the link.xml file can be found in the flappy source code. You may find this issue if you encounter the error “No parameterless constructor defined for Nethereum.Unity.RpcModel.RpcParametersJsonConverter”.

**NOTE** The library uses the custom https://github.com/SaladLab/Json.Net.Unity3D Json.Net library, this is included.
