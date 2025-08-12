using UnityEngine;

public class BettingCube : MonoBehaviour
{
    public enum BetChoice { Over7, Under7, Exactly7 }
    public enum WagerChoice { Percent25, Percent50, Percent100 }

    [Header("Choice Type")]
    public bool isBetCube = true; // Is this for choosing the bet or the wager?
    public BetChoice betType;
    public WagerChoice wagerType;
}