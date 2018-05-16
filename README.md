# Nethereum.Flappy

This is the csharp source code for the Flappy Bird example on how to integrate Unity3d and Ethereum. It is assumed some familiarity with Nethereum as the Unity3d support extends the current implementation.

If you are not familiar with Unity3d this sample is based on the Unity3d tutorial on how to build a flappy bird style game: https://unity3d.com/learn/live-training/session/making-flappy-bird-style-game

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

```csharp
var blockNumberRequest = new EthBlockNumberUnityRequest("https://rinkeby.infura.io");
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
            var blockNumberRequest = new EthBlockNumberUnityRequest ("https://rinkeby.infura.io");
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
var userTopScoreRequest = new EthCallUnityRequest("https://rinkeby.infura.io");
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
The Javascript interop it is achieved using the JsLib (as per Unity3d docs https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html). This is an example of usage:

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
    web3.eth.sendTransaction({from: from, to: tostr, data: datastr} , function(error, hash){  
        if(error){
            console.log(error);
        }
        else {
            console.log(hash);
        }
    }    
    );
  },
});

```

And the source code of the html file which uses the usual web3js and metamask checks

```html
 <script>
        var gameInstance = UnityLoader.instantiate("gameContainer", "Build/webglmeta3.json", {onProgress: UnityProgress});
        window.addEventListener('load', function() {
        // Checking if Web3 has been injected by the browser (Mist/MetaMask)
        if (typeof web3 !== 'undefined') {
          web3.version.getNetwork((err, netId) => {
              if(netId != 4){
                document.getElementById("metamaskWarning").innerText = 'Please connect to Rinkeby to view and submit your top scores';
                web3 = undefined;  
              }else{
                window.web3 = new Web3(web3.currentProvider);
                if(typeof(window.web3.eth.accounts[0]) == 'undefined'){
                  document.getElementById("metamaskWarning").innerText = 'Please unlock Metamask to view and submit your top scores';       
                }else{
                  document.getElementById("metamaskWarning").innerText = '';       
                }
              }
            });
        } else {
          document.getElementById("metamaskWarning").innerText = 'Please install Metamask and connect to Rinkeby to view and submit your top scores';
        }
    });
    </script>
  </head>
  <body>
    <div class="webgl-content">
      <div id="gameContainer" style="width: 960px; height: 600px"></div>
      <div class="footer">
        <div class="webgl-logo"></div> 
        <div class="fullscreen" onclick="gameInstance.SetFullscreen(1)"></div>
        <div class="title">Ethereum Flappy Unicorn PoC using Nethereum, Metamask and Infura</div>
      </div>
       <div class="footer">
        <h2 id="metamaskWarning" style="color:red">Please install Metamask and connect to Rinkeby to submit your top score</h2>
      </div>
      <div class="footer">
        <div class="title">Can you beat your top score and the top 5? Your Top score will be stored in chain once the game is finished</div>
      </div>
      <div class="footer">
        <div class="title">Metamask required with an account in Rinkeby</div>
      </div>
      <div class="footer">
        <div class="title">Contract address: 0x32eb97b8ad202b072fd9066c03878892426320ed</div>
      </div>
      <div class="footer">
        <div class="title"><a href="https://github.com/Nethereum/Nethereum.Flappy/blob/master/playerscore.sol" target="_blank">Contract source</a></div>
      </div>
      
    </div>

```


### Compiling to Webgl and IOS
When compiling to Webgl or IOS, you need to ensure that dlls are not stripped when running IL2CPP. An example of the link.xml file can be found in the flappy source code. You may find this issue if you encounter the error “No parameterless constructor defined for Nethereum.Unity.RpcModel.RpcParametersJsonConverter”.

**NOTE** The library uses the custom https://github.com/SaladLab/Json.Net.Unity3D Json.Net library, this is included.
