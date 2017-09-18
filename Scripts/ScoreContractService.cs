using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.ABI.Encoders;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.JsonRpc.UnityClient;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Signer;
using UnityEngine;


public class ScoreContractService {
    public static string ABI = @"[{'constant':false,'inputs':[{'name':'score','type':'int256'},{'name':'v','type':'uint8'},{'name':'r','type':'bytes32'},{'name':'s','type':'bytes32'}],'name':'setTopScore','outputs':[],'payable':false,'type':'function'},{'constant':true,'inputs':[{'name':'','type':'uint256'}],'name':'topScores','outputs':[{'name':'addr','type':'address'},{'name':'score','type':'int256'}],'payable':false,'type':'function'},{'constant':false,'inputs':[],'name':'getCountTopScores','outputs':[{'name':'','type':'uint256'}],'payable':false,'type':'function'},{'constant':true,'inputs':[{'name':'','type':'address'}],'name':'userTopScores','outputs':[{'name':'','type':'int256'}],'payable':false,'type':'function'},{'inputs':[],'payable':false,'type':'constructor'}]";

    private static string contractAddress = "0x32eb97b8ad202b072fd9066c03878892426320ed";
    private Contract contract;

    public ScoreContractService () {
        this.contract = new Contract (null, ABI, contractAddress);
    }

    public Function GetUserTopScoresFunction () {
        return contract.GetFunction ("userTopScores");
    }

    public Function GetFunctionTopScores () {
        return contract.GetFunction ("topScores");
    }

    public Function GetFunctionGetCountTopScores () {
        return contract.GetFunction ("getCountTopScores");
    }

    public CallInput CreateUserTopScoreCallInput (string userAddress) {
        var
        function = GetUserTopScoresFunction ();
        return function.CreateCallInput (userAddress);
    }

    public CallInput CreateTopScoresCallInput (BigInteger index) {
        var
        function = GetFunctionTopScores ();
        return function.CreateCallInput (index);
    }

    public CallInput CreateCountTopScoresCallInput () {
        var
        function = GetFunctionGetCountTopScores ();
        return function.CreateCallInput ();
    }

    public Function GetFunctionSetTopScore () {
        return contract.GetFunction ("setTopScore");
    }

    public TransactionInput CreateSetTopScoreTransactionInput (string addressFrom, string addressOwner, string privateKey, BigInteger score, HexBigInteger gas = null, HexBigInteger valueAmount = null) {
        var numberBytes = new IntTypeEncoder ().Encode (score);
        var sha3 = new Nethereum.Util.Sha3Keccack ();
        var hash = sha3.CalculateHashFromHex (addressFrom, addressOwner, numberBytes.ToHex ());
        var signer = new MessageSigner ();
        var signature = signer.Sign (hash.HexToByteArray (), privateKey);
        var ethEcdsa = MessageSigner.ExtractEcdsaSignature (signature);

        var
        function = GetFunctionSetTopScore ();
        return function.CreateTransactionInput (addressFrom, gas, valueAmount, score, ethEcdsa.V, ethEcdsa.R, ethEcdsa.S);
    }

    public int DecodeUserTopScoreOutput (string result) {
        var
        function = GetUserTopScoresFunction ();
        return function.DecodeSimpleTypeOutput<int> (result);
    }

    public int DecodeTopScoreCount (string result) {
        var
        function = GetFunctionGetCountTopScores ();
        return function.DecodeSimpleTypeOutput<int> (result);
    }

    public TopScoresDTO DecodeTopScoreDTO (string result) {
        var
        function = GetFunctionTopScores ();
        return function.DecodeDTOTypeOutput<TopScoresDTO> (result);
    }
}

[FunctionOutput]
public class TopScoresDTO {
    [Parameter ("address", "addr", 1)]
    public string Addr { get; set; }

    [Parameter ("int256", "score", 2)]
    public BigInteger Score { get; set; }

}