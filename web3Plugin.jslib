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
