﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.Model;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.JsonRpc.UnityClient;
using Nethereum.Util;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TopScoreService : MonoBehaviour {
    //This is the Text that displays the user top score
    public Text topScoreText;
    public Text userAddress;
    public Text topScoresAllTimeText;

    private string _addressOwner = "0x12890d2cce102216644c59daE5baed380d84830c";
    private string _userAddress; // = 
    private byte[] _key;
    //Partial private key to sign the transactions
    private string _privateKey = "fa002a6a5bc0f42cc9a8806ab109bf5cd2f8bb6c54d4";
    //Url to Ethereum public client (Todo support https for Infura)
    private string _url = "https://rinkeby.infura.io";
    //Service to generate, encode and decode CallInput and TransactionInpput
    //This includes the contract address, abi,, etc, similar to a generic Nethereum services
    private ScoreContractService _scoreContractService;

#if !UNITY_EDITOR
    public bool ExternalProvider = true;
    [DllImport ("__Internal")]
    private static extern string GetAccount ();
    [DllImport ("__Internal")]
    private static extern string SendTransaction (string to, string data);

#else
    public bool ExternalProvider = false;
    private static string GetAccount () { return null; }
    private static string SendTransaction (string to, string data) { return null; }
#endif

    private bool submitting = false;

    void Start () {
        _scoreContractService = new ScoreContractService ();

        //Coroutines
        StartCoroutine (GetExternalAccount ());
        StartCoroutine (GetTopScores ());
        StartCoroutine (CheckTopScore ());
        StartCoroutine (CheckSubmitScore ());
    }

    public IEnumerator GetTopScores () {
        var wait = 0;
        while (true) {
            yield return new WaitForSeconds (wait);
            wait = 20;

            //Create a unity call request (we have a request for each type of rpc operation)
            var topScoreRequest = new EthCallUnityRequest (_url);

            //Use the service to create a call input which includes the encoded  
            var countTopScoresCallInput = _scoreContractService.CreateCountTopScoresCallInput ();
            //Call request sends and yield for response	
            yield return topScoreRequest.SendRequest (countTopScoresCallInput, Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest ());

            //decode the top score using the service
            var scores = new List<TopScoresDTO> ();

            var count = _scoreContractService.DecodeTopScoreCount (topScoreRequest.Result);
            for (int i = 0; i < count; i++) {
                topScoreRequest = new EthCallUnityRequest (_url);
                var topScoreCallInput = _scoreContractService.CreateTopScoresCallInput (i);
                yield return topScoreRequest.SendRequest (topScoreCallInput, Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest ());

                scores.Add (_scoreContractService.DecodeTopScoreDTO (topScoreRequest.Result));
            }

            var orderedScores = scores.OrderByDescending (x => x.Score).ToList ();

            var topScores = "Top Scores" + Environment.NewLine;

            foreach (var score in orderedScores) {
                topScores = topScores + score.Score + "-" + score.Addr.Substring (0, 15) + "..." + Environment.NewLine;

            }
            topScoresAllTimeText.text = topScores;
        }

    }

    //This will be used to get the account using metamask (uport?), we loop to check for account changes on metamask
    public IEnumerator GetExternalAccount () {
        if (ExternalProvider) {
            var wait = 2;
            while (true) {
                yield return new WaitForSeconds (wait);
                wait = 20;
                var acc = GetAccount ();
                if (acc == "") {
                    _userAddress = null;
                } else {
                    _userAddress = acc;
                }

                userAddress.text = _userAddress;
            }
        }
    }

    //Making a contract call:

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

    //Making  a transaction signed raw request
    public IEnumerator CheckSubmitScore () {
        var wait = 4;
        while (true) {
            yield return new WaitForSeconds (wait);

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
    }
}

public class UserTopScoreRequest : UnityRequest<int> {
    private ScoreContractService _scoreContractService;
    private string _url;

    public UserTopScoreRequest (ScoreContractService scoreRequestBuilder, string url) {
        _scoreContractService = scoreRequestBuilder;
        _url = url;
    }

    public IEnumerator SendGetUserTopScoreRequest (string userAddress) {
        var unityClientService = new EthCallUnityRequest (_url);
        yield return unityClientService.SendRequest (_scoreContractService.CreateUserTopScoreCallInput (userAddress), Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest ());

        Exception = unityClientService.Exception;

        if (!String.IsNullOrEmpty (unityClientService.Result)) {
            Debug.Log (unityClientService.Result);
            Result = _scoreContractService.DecodeUserTopScoreOutput (unityClientService.Result);
        }
    }
}