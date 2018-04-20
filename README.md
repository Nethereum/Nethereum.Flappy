# Nethereum.Flappy

This is the csharp source code for the Flappy Bird example on how to integrate Unity3d and Ethereum. It is assumed some familiarity with Nethereum as the Unity3d support extends the current implementation.

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

To achieve this you can create your own external library to interact with the injected web3 library in the browser. For more information on External libraries check the Unity documentation

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


### Compiling to Webgl and IOS
When compiling to Webgl or IOS, you need to ensure that dlls are not stripped when running IL2CPP. An example of the link.xml file can be found in the flappy source code. You may find this issue if you encounter the error “No parameterless constructor defined for Nethereum.Unity.RpcModel.RpcParametersJsonConverter”.

**NOTE** The library uses the custom https://github.com/SaladLab/Json.Net.Unity3D Json.Net library, this is included.
