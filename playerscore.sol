pragma solidity ^0.4.10

contract PlayerScore {
    
    uint maxTopScores = 5;
    address owner;

    struct TopScore{
        address addr;
        int score;
    }
    
    function PlayerScore(){
        owner = msg.sender;
    }

    TopScore[] public topScores;

    mapping (address=>int) public userTopScores;
    
    function setTopScore(int256 score, uint8 v, bytes32 r, bytes32 s) {
        var hash = sha3(msg.sender, owner, score);
        var addressCheck = ecrecover(hash, v, r, s);
        
        if(addressCheck != owner) throw;
        
        var currentTopScore = userTopScores[msg.sender];
        if(currentTopScore < score){
            userTopScores[msg.sender] = score;
        }

        if(topScores.length < maxTopScores){
            var topScore = TopScore(msg.sender, score);
            topScores.push(topScore);
        }else{
            int lowestScore = 0;
            uint lowestScoreIndex = 0; 
            for (uint i = 0; i < topScores.length; i++)
            {
                TopScore currentScore = topScores[i];
                if(i == 0){
                    lowestScore = currentScore.score;
                    lowestScoreIndex = i;
                }else{
                    if(lowestScore > currentScore.score){
                        lowestScore = currentScore.score;
                        lowestScoreIndex = i;
                    }
                }
            }
            if(score > lowestScore){
                var newtopScore = TopScore(msg.sender, score);
                topScores[lowestScoreIndex] = newtopScore;
            }
        }
    }

    function getCountTopScores() returns(uint) {
        return topScores.length;
    }
}
