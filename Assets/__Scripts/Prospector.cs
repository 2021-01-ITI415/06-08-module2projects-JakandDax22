using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class Prospector : MonoBehaviour {

	static public Prospector S;

	[Header("Set in Inspector")]
	public TextAsset deckXML;
	public TextAsset layoutXML;
	public float xOffset = 3;
	public float yOffset = -2.5f;
	public Vector3 layoutCenter;


	[Header("Set Dynamically")]
	public Deck deck;
	public Layout layout;
	public List<CardProspector> drawPile;
	public Transform layoutAnchor;
	public CardProspector target;
	public List<CardProspector> tableau;
	public List<CardProspector> discardPile;

	void Awake() {
		S = this;
	}

	void Start() {
		deck = GetComponent<Deck>();
		deck.InitDeck(deckXML.text);
		Deck.Shuffle(ref deck.cards); //This shuffles the deck by references

		Card c;
		for (int cNum = 0; cNum < deck.cards.Count; cNum++)
		{
			c = deck.cards[cNum];
			c.transform.localPosition = new Vector3((cNum % 13) * 3, cNum / 13 * 4, 0);
		}
		layout = GetComponent<Layout>(); //Get the Layout component
		layout.ReadLayout(layoutXML.text); //Pass the LayoutXML to it
		drawPile = ConvertListCardsToListCardProspectors(deck.cards);
		LayoutGame();
	}

	List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lCD){
		List<CardProspector> lCP = new List<CardProspector>();
		CardProspector tCP;
		foreach(Card tCD in lCD)
        {
			tCP = tCD as CardProspector;
			lCP.Add(tCP);
        }
		return (lCP);
	}
	//The draw function will pull a single card from the drawPile and retuns it
	CardProspector Draw()
    {
		CardProspector cd = drawPile[0]; //Pull the 0th CardProspector
		drawPile.RemoveAt(0); //Then remove it from the List<> drawpile
		return (cd); //And return it
    }

	//LayoutGame() position the intial tableau of cards, a.k.a the "mine"
	void LayoutGame(){
		//Create an empty GameObject to serve as an anchor for the tableau
		if (layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
			//^ Create an empty GameObject named _LayoutAnchor in the heirarchy
			layoutAnchor = tGO.transform; //Grab its Transform
			layoutAnchor.transform.position = layoutCenter; //Position it
        }

		CardProspector cp;
		//Follow the layout
		foreach (SlotDef tSD in layout.slotDefs)
		{
			//^ Iterate through all the slotdefs in the layout.slotdefs as tSD
			cp = Draw(); //Pull a card from the top (beginning) of the draw pile
			cp.faceUp = tSD.faceUp; //Set its faceup to the value in slotdef
			cp.transform.parent = layoutAnchor; //Make its parent layoutAnchor
			//This replaces the previous parent, deck.deckAnchor, which
			//appears as _Deck in the Heirarchy when the scene is playing
			cp.transform.localPosition = new Vector3(
				layout.multiplier.x * tSD.x,
				layout.multiplier.y * tSD.y,
				- tSD.layerID);
			//^ Set the localPosition of the card based on slotDef
			cp.layoutID = tSD.id;
			cp.slotDef = tSD;
			//CardProspectors in the tableau have the state CardState.tableau
			cp.state = eCardState.tableau;
			cp.SetSortingLayerName(tSD.layerName); //Set the sorting layers

			tableau.Add(cp); //Add this CardProspector to the List<> tableau
			
		}

		//Set up the initial target card
		MoveToTarget(Draw());
		//Set up the draw pile
		UpdateDrawPile();
	}

	//Moves the current target to discardPile
	void MoveToDiscard(CardProspector cd)
    {
		//Set the state of the card to discard
		cd.state = eCardState.discard;
		discardPile.Add(cd); //Add it to the discardPile List<>
		cd.transform.parent = layoutAnchor; //Update its transform parent

		//Position this card on the discardPile
		cd.transform.localPosition = new Vector3(
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerID + 0.5f);
		cd.faceUp = true;
		//Place it on top of the pile for depth sorting
		cd.SetSortingLayerName(layout.discardPile.layerName);
		cd.SetSortingOrder(-100+discardPile.Count);
    }

	void MoveToTarget(CardProspector cd)
    {
		//If there is currently a target card, move it to discardPile
		if (target != null) MoveToDiscard(target);
		target = cd; //cd is the new target
		cd.state = eCardState.target;
		cd.transform.parent = layoutAnchor;
		//Move to the target position
		cd.transform.localPosition = new Vector3(
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerID);

		cd.faceUp = true; //Make it face up
						  //Set the depth sorting
		cd.SetSortingLayerName(layout.discardPile.layerName);
		cd.SetSortingOrder(0);
    }

	//Arranges all the cards of the drawPile to show how many are left
	void UpdateDrawPile()
    {
		CardProspector cd;
		//Go through all of the cards of the drawPile
		for(int i=0; i<drawPile.Count; i++)
        {
			cd = drawPile[i];
			cd.transform.parent = layoutAnchor;

			//Position it correctly with the layout.drawPile.stagger
			Vector2 dpStagger = layout.drawPile.stagger;
			cd.transform.localPosition = new Vector3(
			layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x),
			layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y),
			-layout.drawPile.layerID + 0.1f*i);

			cd.faceUp = false; //Make them all face down
			cd.state = eCardState.drawpile;
			//Set the depth sorting
			cd.SetSortingLayerName(layout.drawPile.layerName);
			cd.SetSortingOrder(-10 * i);
        }
    }

	//CardClicked is called any time a card in the game is clicked
	public void CardClicked(CardProspector cd)
    {
		//The reaction os determined by the state of the clicked card
		switch (cd.state)
        {
			case eCardState.target:
				//Clicking the target does nothing
				break;

			case eCardState.drawpile:
				//Clicking any card in the drawPile will draw the next card
				MoveToDiscard(target); //Moves the target to the discardPile
				MoveToTarget(Draw()); //Moves the next drawn card to the target
				UpdateDrawPile(); //Restacks the drawPile
				break;

			case eCardState.tableau:
				//Clicking a card in the tableau will check if it's a valid play
				bool validMatch = true;
				if (!cd.faceUp)
                {
					//If the card is face-down, it's not valid
					validMatch = false;
                }
				if (!AdjacentRank(cd, target))
                {
					//If it's not adjacent rank, it's not valid
					validMatch = false;
                }
				if (!validMatch) return; //return if not valid

				//If we got here, then: Yay! It's a valid card.
				tableau.Remove(cd); //Remove it from the tableau list
				MoveToTarget(cd); //Make it the target card
				break;
        }
    }

	//Returns true if the two cards are adjacent in rank (A & K around)
	public bool AdjacentRank(CardProspector c0, CardProspector c1)
    {
		//If either card is face-down, it's not adjacent.
		if (!c0.faceUp || !c1.faceUp) return (false);

		//If they are one apart, they are adjacent
		if (Mathf.Abs(c0.rank - c1.rank) == 1)
        {
			return (true);
        }

		//If one is Ace and the other is King, they are adjacent
		if (c0.rank == 1 && c1.rank == 13) return (true);
		if (c0.rank == 13 && c1.rank == 1) return (true);

		//Otherwise, return false
		return (false);
	}
}
