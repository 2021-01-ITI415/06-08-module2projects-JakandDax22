using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//An enum defines a variable type with a few prenamed values
/*
public enum eCardState
{
    drawpile,
    tableau,
    target,
    discard
}
*/

public class CardGolf : Card
{
    //Make sure CardProspector extends Card
    [Header("Set Dynamically: CardGolf")]
    public bool isGold = false;
    //This is how you use the enum eCardState
    public eCardState       state = eCardState.drawpile;
    //The hiddenby list stores which other cards will keep this one face down
    public List<CardGolf>     hiddenBy = new List<CardGolf>();
    //The LayoutID matches this card to the tableau XML if it's a tableau card
    public int layoutID;
    //The SlotDef class stores information pulled from the LayoutXML <slot>
    public SlotDef slotDef;

    //This allows the card to react to being clicked
    override public void OnMouseUpAsButton()
    {
        //Call the CardClicked method on the Prospector singleton
        Golf.S.CardClicked(this);
        //Also call the base class (Card.cs) version of this method
        base.OnMouseUpAsButton();
    }
}
