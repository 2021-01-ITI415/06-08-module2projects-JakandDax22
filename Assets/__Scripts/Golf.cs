using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class Golf : MonoBehaviour {

	static public Golf S;

	[Header("Set in Inspector")]
	public TextAsset deckXML;
	public TextAsset layoutXML;
	public float xOffset = 3;
	public float yOffset = -2.5f;
	public Vector3 layoutCenter;
	public Vector2 fsPosMid = new Vector2(0.5f, 0.90f);
	public Vector2 fsPosRun = new Vector2(0.5f, 0.75f);
	public Vector2 fsPosMid2 = new Vector2(0.4f, 1.0f);
	public Vector2 fsPosEnd = new Vector2(0.5f, 0.95f);
	public float reloadDelay = 2f; //2 sec delay between rounds
	public Text gameOverText, roundResultText, highScoreText;


	[Header("Set Dynamically")]
	public Deck deck;
	public Layout layout;
	public List<CardGolf> drawPile;
	public Transform layoutAnchor;
	public CardGolf target;
	public List<CardGolf> tableau;
	public List<CardGolf> discardPile;
	public FloatingScore fsRun;

	void Awake() {
		S = this;
		SetUpUITexts();
	}

	void Start() {
		Scoreboard.S.score = ScoreManager.SCORE;

		deck = GetComponent<Deck>(); //Get the deck
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
		drawPile = ConvertListCardsToListCardGolf(deck.cards);
		LayoutGame();
	}

	List<CardGolf> ConvertListCardsToListCardGolf(List<Card> lCD){
		List<CardGolf> lCP = new List<CardGolf>();
		CardGolf tCP;
		foreach(Card tCD in lCD)
        {
			tCP = tCD as CardGolf;
			lCP.Add(tCP);
        }
		return (lCP);
	}
	//The draw function will pull a single card from the drawPile and retuns it
	CardGolf Draw()
    {
		CardGolf cd = drawPile[0]; //Pull the 0th CardProspector
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

		CardGolf cp;
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

        //Set which cards are hiding others
        foreach (CardGolf tCP in tableau)
        {
            foreach (int hid in tCP.slotDef.hiddenBy)
            {
				cp = FindCardByLayoutID(hid);
				tCP.hiddenBy.Add(cp);
			}
            if (Random.Range(0, 100)<=10)
            {
				tCP.isGold = true;
				tCP.GetComponent<SpriteRenderer>().sprite= Camera.main.GetComponent<Deck>().cardFrontGold;
				Transform tCardBack = tCP.transform.Find("back");
				tCardBack.gameObject.GetComponent<SpriteRenderer>().sprite = Camera.main.GetComponent<Deck>().cardBackGold;
            }
        }

		//Set up the initial target card
		MoveToTarget(Draw ());
		//Set up the draw pile
		UpdateDrawPile();
	}

	//Convert from layoutID int to the CardProspector with that ID
	CardGolf FindCardByLayoutID(int layoutID)
    {
		foreach(CardGolf tCP in tableau)
        {
			//Search through all cards in the tableau List<>
			if(tCP.layoutID == layoutID)
            {
				//If the card has the same ID, return it
				return (tCP);
            }
        }
		//If it's not found, return null
		return (null);
    }

	//This turns cards in the Mine face-up or face-down
	void SetTableauFaces()
    {
		foreach(CardGolf cd in tableau)
        {
			bool faceUp = true; //Assume the card will be face-up
            foreach (CardGolf cover in cd.hiddenBy)
            {
                //If either of the covering cards are in the tableau
                if (cover.state == eCardState.tableau)
                {
					faceUp = false; //then this card is face-down
                }
            }
			cd.faceUp = faceUp; // Set the value on the card
        }
    }

	//Moves the current target to discardPile
	void MoveToDiscard(CardGolf cd)
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

	void MoveToTarget(CardGolf cd)
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
		CardGolf cd;
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
	public void CardClicked(CardGolf cd)
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
				ScoreManager.EVENT(eScoreEvent.draw);
				FloatingScoreHandler(eScoreEvent.draw);
				break;

			case eCardState.tableau:
				//Clicking a card in the tableau will check if it's a valid play
				bool validMatch = true;
				bool goldClicked = false;
				//Check to see if a gold card was clicked
				
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
                if (cd.isGold == true)
                {
					goldClicked = true;
                }
				tableau.Remove(cd); //Remove it from the tableau list
				MoveToTarget(cd); //Make it the target card
				SetTableauFaces(); //Update tableau card face-ups
                if (goldClicked)
                {
					ScoreManager.EVENT(eScoreEvent.mineGold);
					FloatingScoreHandler(eScoreEvent.mineGold);
				}
                else
                {
					ScoreManager.EVENT(eScoreEvent.mine);
					FloatingScoreHandler(eScoreEvent.mine);
				}
				
				break;
        }
		//Check to see whether the game is over or not
		CheckForGameOver();
    }

	//Test whether the game is over
	void CheckForGameOver()
    {
		//If tableau is empty, the game is over
		if(tableau.Count == 0)
        {
			//Call GameOver() with a win
			GameOver (true);
			return;
        }

        //If there are still cards in the draw pile, the game's not over
        if (drawPile.Count > 0)
        {
			return;
        }

		//Check for remaining valid plays
		foreach(CardGolf cd in tableau)
        {
            if (AdjacentRank(cd, target))
            {
				//If there is a valid play, the game is not over
				return;
            }
        }

		//Since there are no valid plays, the game is over
		//Call GameOver with a loss
		GameOver(false);
    }

	void GameOver(bool won)
    {
		int score = ScoreManager.SCORE;
		if (fsRun != null) score += fsRun.score;
		if (won)
        {
			//print("Game Over. You win! :^]");
			gameOverText.text = "Round Over";
			roundResultText.text = "You won this round!\nRound Score: " +score;
			ShowResultsUI(true);
			ScoreManager.EVENT(eScoreEvent.gameWin);
			FloatingScoreHandler(eScoreEvent.gameWin);

		}
        else
        {
			//print("Game Over. You Lost. >:^[");
			gameOverText.text = "Game Over";
			if (ScoreManager.HIGH_SCORE <= score)
            {
				string str = "You got the high score! \nHigh score: " + score;
				roundResultText.text = str;
            }
            else
            {
				roundResultText.text = "Your final score was: " + score;
            }
			ShowResultsUI(true);
			ScoreManager.EVENT(eScoreEvent.gameLoss);
			FloatingScoreHandler(eScoreEvent.gameLoss);

		}
		//Reload the scene, resetting the game
		//SceneManager.LoadScene("__Prospector_Scene_0");

		//Reload this scene in reloadDelay seconds
		//This will give the score a moment to travel
		Invoke("ReloadLevel", reloadDelay);
    }

	void ReloadLevel()
    {
		//Reload the scene, resetting the game
		SceneManager.LoadScene("__Golf_Scene_0");
	}

	//Returns true if the two cards are adjacent in rank (A & K around)
	public bool AdjacentRank(CardGolf c0, CardGolf c1)
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

	//Handle FloatingScore movement
	void FloatingScoreHandler(eScoreEvent evt)
    {
		List<Vector2> fsPts;
        switch (evt)
        {
			//Same things need to happen whether it's a draw, a win, or a loss
			case eScoreEvent.draw: //Drawing a card
			case eScoreEvent.gameWin: //Won the round
			case eScoreEvent.gameLoss: //Lost the round
                                       //Add fsRun to the scoreboard score
                if (fsRun != null)
                {
					//Create points for the Bezier curve
					fsPts = new List<Vector2>();
					fsPts.Add(fsPosRun);
					fsPts.Add(fsPosMid2);
					fsPts.Add(fsPosEnd);
					fsRun.reportFinishTo = Scoreboard.S.gameObject;
					fsRun.Init(fsPts, 0, 1);
					//Also adjust the fontSize
					fsRun.fontSizes = new List<float>(new float[] { 28, 36, 4 });
					fsRun = null; //Clear fsRun so it's created again
				}
				break;

			case eScoreEvent.mine: //Remove a mine card
								   //Create a FloatingScore for this score
				FloatingScore fs;
				//Move it from the mousePosition to fsPosRun
				Vector2 p0 = Input.mousePosition;
				p0.x /= Screen.width;
				p0.y /= Screen.height;
				fsPts = new List<Vector2>();
				fsPts.Add(p0);
				fsPts.Add(fsPosMid);
				fsPts.Add(fsPosRun);
				fs = Scoreboard.S.CreateFloatingScore(ScoreManager.CHAIN, fsPts);
				fs.fontSizes = new List<float>(new float[] { 4, 50, 28 });
                if (fsRun == null)
                {
					fsRun = fs;
					fsRun.reportFinishTo = null;
                }
                else
                {
					fs.reportFinishTo = fsRun.gameObject;
                }
				break;

			case eScoreEvent.mineGold: //Remove a mine card
								   //Create a FloatingScore for this score
				//FloatingScore fs;
				//Move it from the mousePosition to fsPosRun
				p0 = Input.mousePosition;
				p0.x /= Screen.width;
				p0.y /= Screen.height;
				fsPts = new List<Vector2>();
				fsPts.Add(p0);
				fsPts.Add(fsPosMid);
				fsPts.Add(fsPosRun);
				fs = Scoreboard.S.CreateFloatingScore(ScoreManager.SCORE_RUN, fsPts);
				fs.fontSizes = new List<float>(new float[] { 4, 50, 28 });
				if (fsRun == null)
				{
					fsRun = fs;
					fsRun.reportFinishTo = null;
				}
				else
				{
					fs.reportFinishTo = fsRun.gameObject;
				}
				break;
		}
    }

	void SetUpUITexts()
    {
		//Set up the HighScore UI Text
		GameObject go = GameObject.Find("HighScore");
		if(go != null)
        {
			highScoreText = go.GetComponent<Text>();
        }
		int highScore = ScoreManager.HIGH_SCORE;
		string hScore = "High Score: " + Utils.AddCommasToNumber(highScore);
		go.GetComponent<Text>().text = hScore;

		//Set up the UI Texts that show at the end of the round
		go = GameObject.Find("GameOver");
        if (go != null)
        {
			gameOverText = go.GetComponent<Text>();
        }
		go = GameObject.Find("RoundResult");
		if (go != null)
		{
			roundResultText = go.GetComponent<Text>();
		}

		//Make the end of round texts invisible
		ShowResultsUI(false);
	}

	void ShowResultsUI(bool show)
	{
		gameOverText.gameObject.SetActive(show);
		roundResultText.gameObject.SetActive(show);
	}

}
