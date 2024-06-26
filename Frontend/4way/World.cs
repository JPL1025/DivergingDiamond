using Godot;
using System;
using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Text.Json;
//using System.Text.Json.Serialization;

public class World : Node2D
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";
	private PackedScene ballScene = GD.Load<PackedScene>("res://Ball.tscn");
	private PackedScene transition = GD.Load<PackedScene>("res://Transition.tscn");
	private HTTPRequest _httpRequest;
	private int[,] board;
	
	private int debugBall = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		board = new int[7, 7];
		
		_httpRequest = GetNode<HTTPRequest>("HTTPRequest");
		
		string url = "https://fourway-backend.onrender.com/get-json";
		FetchJSON(url);
		
		/*
		if (ParseJSON("res://backup.json") == false) {
			for (int i = 0; i < 7; i++) {
				for (int j = 0; j < 7; j++) {
					board[i, j] = 0;
				}
			}
		}
		*/
	}
	
	
	private void FirstBalls() {
		AddBall();
		AddBall();
		
		printBoard();
	}
	
	private void FetchJSON(string url) {
		GD.Print("Fetching JSON!");
		var error = _httpRequest.Request(url);
		if (error != Error.Ok) {
			GD.PrintErr("Failed to send the HTTP request: ", error);
			debugBall = 10;
			Backup();
		}
	}
	
	private void _on_HTTPRequest_request_completed(int result, int responseCode, string[] headers, byte[] body) {
		GD.Print("Request Completed!");
		if (responseCode != 200) {
			GD.PrintErr("HTTP request failed - code: ", responseCode);
			debugBall = 11;
			Backup();
			return;
		}
		string jsonString = System.Text.Encoding.UTF8.GetString(body);
		ParseJSON(jsonString);
	}
	
	/*
	private void OnRequestCompleted(int result, int responseCode, string[] headers, byte[] body) {

	}
	*/
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(float delta) {
		if (Input.IsActionJustPressed("ui_up")) {
			up();
		}
		if (Input.IsActionJustPressed("ui_down")) {
			down();
		}
		if (Input.IsActionJustPressed("ui_left")) {
			left();
		}
		if (Input.IsActionJustPressed("ui_right")) {
			right();
		}
	}
	
	// Places the new sprite on the board
	private void placeSprite(int y, int x, int size) {
		string path = "Balls/x" + x + "/y" + y;
		string texturePath = "res://Sprites/" + size.ToString() + ".png";
		
		if (size < 0) { // -1, unusable space
			texturePath = "res://Sprites/unused_tile.png";
		}
		
		Node target = GetNode(path);
		if (target == null) {
			GD.PrintErr("Node not found: " + target);
			return;
		}
		
		Sprite spr = new Sprite();
		spr.Texture = (Texture)GD.Load(texturePath);
		spr.Name = size.ToString();
		
		spr.Position = new Vector2((x-3)*21, (y-3)*21);
		target.AddChild(spr);
	}
	
	private void PrintTree(Node node, int level)
	{
		// Print the current node's name with indentation based on its level in the tree
		GD.Print(new String(' ', level * 2) + node.Name);

		// Recursively print each child node
		foreach (Node child in node.GetChildren())
		{
			PrintTree(child, level + 1);
		}
	}
	
	private void moveSprite(int y1, int x1, int y2, int x2, int size) {
		
		string oldPath = "Balls/x" + x1 + "/y" + y1 + "/" + size;
		string newPath = "Balls/x" + x2 + "/y" + y2;
		
		Sprite spriteMove = GetNode<Sprite>(oldPath);
		Node newParent = GetNode(newPath);
		
		if (spriteMove == null) {
			GD.PrintErr("Sprite not found: " + oldPath);
			return;
		}
		if (newParent == null) {
			GD.PrintErr("New parent not found: " + newParent);
			return;
		}
		
		// Remove the sprite from its old parent
		spriteMove.GetParent().RemoveChild(spriteMove);
		
		spriteMove.Position = new Vector2((x2-3)*21, (y2-3)*21);
		
		newParent.AddChild(spriteMove);
	}
	
	// Merge the sprite into the other, then delete it
	private void mergeSprites(int y1, int x1, int y2, int x2, int oldSize) {
		
		string oldPath = "Balls/x" + x1 + "/y" + y1 + "/" + oldSize;
		string newPath = "Balls/x" + x2 + "/y" + y2 + "/" + oldSize;
		string texturePath = "res://Sprites/" + (oldSize + 1).ToString() + ".png";
		
		Sprite spriteMove = GetNode<Sprite>(oldPath);
		Sprite spriteStay = GetNode<Sprite>(newPath);
		
		spriteMove.GetParent().RemoveChild(spriteMove);
		spriteMove.QueueFree();
		
		spriteStay.Texture = (Texture)GD.Load(texturePath);
		spriteStay.Name = (oldSize + 1).ToString();
	}

	private bool ParseJSON(string jsonString) {
		JSONParseResult jsonResult = JSON.Parse(jsonString);

		if (jsonResult.Error != Error.Ok) {
			GD.PrintErr("JSON could not be parsed: ", jsonResult.ErrorString);
			debugBall = 12;
			Backup();
			return false;
		}

		// Validate the JSON result
		if (!(jsonResult.Result is Godot.Collections.Dictionary jsonData)) {
			GD.PrintErr("JSON content is not a dictionary");
			debugBall = 13;
			Backup();
			return false;
		}

		if (!jsonData.Contains("booleanArray") || !(jsonData["booleanArray"] is Godot.Collections.Array dataArray)) {
			GD.PrintErr("JSON does not contain a valid 'booleanArray'");
			debugBall = 14;
			Backup();
			return false;
		}

		int i = 0;
		foreach (Godot.Collections.Array row in dataArray) {
			if (!(row is Godot.Collections.Array boolRow)) {
				GD.PrintErr("Row is not a valid array");
				debugBall = 15;
				Backup();
				return false;
			}

			int j = 0;
			foreach (var val in boolRow) {
				if (!(val is bool)) {
					GD.PrintErr("Array element is not a boolean");
					debugBall = 16;
					Backup();
					return false;
				}

				if ((bool)val) {
					board[i, j] = 0;
				}
				else {
					board[i, j] = -1;
					placeSprite(i, j, -1);
				}
				j++;
			}
			i++;
		}
		
		FirstBalls();

		return true;
	}
	
	
	// Runs if it cannot connect to the server
	private void Backup() {
		GD.Print("Backup!");
		
		for (int j = 0; j < 7; j++) {
			for (int i = 0; i < 2; i++) {
				board[i, j] = -1;
				placeSprite(i, j, -1);
			}
			for (int i = 5; i < 7; i++) {
				board[i, j] = -1;
				placeSprite(i, j, -1);
			}
		}

		for (int i = 2; i < 5; i++) {
			board[i, 0] = -1;
			board[i, 6] = -1;
			placeSprite(i, 0, -1);
			placeSprite(i, 6, -1);
			for (int j = 1; j < 6; j++) {
				board[i, j] = 0;
			}
		}
		
		FirstBalls();
	}
		
	/*
	private bool BackupJSON(string path) {
		
		// Open file and turn contents into a string
		var file = new File();
		if (!file.FileExists(path)) {
			GD.PrintErr("JSON File not found: ", path);
			return false;
		}
		
		file.Open(path, File.ModeFlags.Read);
		string jsonString = file.GetAsText();
		file.Close();
		
		JSONParseResult jsonResult = JSON.Parse(jsonString);
		
		if (jsonResult.Error != Error.Ok) {
			GD.PrintErr("JSON File could not be parsed: ", jsonResult.ErrorString);
			return false;
		}
		
		Godot.Collections.Dictionary jsonData = (Godot.Collections.Dictionary)jsonResult.Result;
		Godot.Collections.Array dataArray = (Godot.Collections.Array)jsonData["booleanArray"];
		
		int i = 0;
		
		foreach(Godot.Collections.Array row in dataArray) {
			int j = 0;
			foreach (bool val in row) {
				if (val) { //True
					board[i, j] = 0;
				} else {
					board[i, j] = -1;
					placeSprite(i, j, -1);
				}
				j++;
			}
			i++;
		}
		
		return true;
	}
	*/

	private void AddBall() {
		List<int> zeroIndices = new List<int>();
		
		// TODO I could probably optimize this so it doesn't need to run every time a ball is added
		for (int i = 0; i < 7; i++) {
			for (int j = 0; j < 7; j++) {
				if (board[i, j] == 0) {
					zeroIndices.Add((i * 7) + j);
				}
			}
		}
		
		if (zeroIndices.Count <= 0) {
			return;
		}
		
		Random rand = new Random();
		int y = zeroIndices[rand.Next(zeroIndices.Count)];
		
		int x = y % 7;
		y /= 7;
		
		// Chances for the newly made ball to be a certain number
		int[] chances = {1, 1, 1, 1, 1, 1, 2, 2, 2, 3};
		int index = rand.Next(0, 10);
		
		if (debugBall != 0) {
			board[y, x] = debugBall;
			placeSprite(y, x, debugBall);
			return;
		}
		board[y, x] = chances[index];
		placeSprite(y, x, chances[index]);
	}
	
	
	private void up() {
		// Store the next available space for the ball to move
		int[] nextFree = {0, 0, 0, 0, 0, 0, 0};
		
		// Set merged array
		bool[,] merged = new bool[7, 7];
		for (int i = 0; i < 7; i++) {
			for (int j = 0; j < 7; j++) {
				merged[i, j] = false;
			}
		}
		
		// Check first row
		for (int j = 0; j < 7; j++) {
			if (board[0, j] != 0) {
				nextFree[j] = 1;
			}
		}
		
		// Check other rows, move and merge if possible
		for (int i = 1; i < 7; i++) {
			for (int j = 0; j < 7; j++) {
				if (board[i, j] > 0) {
					// Merge
					if (nextFree[j] > 0 && board[i, j] == board[nextFree[j] - 1, j] && merged[nextFree[j] - 1, j] == false) {
						mergeSprites(i, j, nextFree[j] - 1, j, board[i, j]);
						board[nextFree[j] - 1, j]++;
						merged[nextFree[j] - 1, j] = true;
						board[i, j] = 0;
					// Move
					} else if (nextFree[j] < i) {
						board[nextFree[j], j] = board[i, j];
						board[i, j] = 0;
						moveSprite(i, j, nextFree[j], j, board[nextFree[j], j]);
						nextFree[j]++;
					// Space taken
					} else {
						nextFree[j]++;
					}
				} else if (board[i, j] < 0) { // -1 == non-usable space
					nextFree[j] = i + 1;
				}
			}
		}
		
		AddBall();
		printBoard();
		CheckGameOver();
	}
	
	private void down() {
		// Store the next available space for the ball to move
		int[] nextFree = {6, 6, 6, 6, 6, 6, 6};
		
		// Set merged array
		bool[,] merged = new bool[7, 7];
		for (int i = 0; i < 7; i++) {
			for (int j = 0; j < 7; j++) {
				merged[i, j] = false;
			}
		}
		
		// Check first row
		for (int j = 0; j < 7; j++) {
			if (board[6, j] != 0) {
				nextFree[j] = 5;
			}
		}
		
		// Check other rows, move and merge if possible
		for (int i = 5; i >= 0; i--) {
			for (int j = 0; j < 7; j++) {
				if (board[i, j] > 0) {
					// Merge
					if (nextFree[j] < 6 && board[i, j] == board[nextFree[j] + 1, j] && merged[nextFree[j] + 1, j] == false) {
						mergeSprites(i, j, nextFree[j] + 1, j, board[i, j]);
						board[nextFree[j] + 1, j]++;
						merged[nextFree[j] + 1, j] = true;
						board[i, j] = 0;
					// Move
					} else if (nextFree[j] > i) {
						board[nextFree[j], j] = board[i, j];
						board[i, j] = 0;
						moveSprite(i, j, nextFree[j], j, board[nextFree[j], j]);
						nextFree[j]--;
					// Space Taken
					} else {
						nextFree[j]--;
					}
				} else if (board[i, j] < 0) { // -1 == non-usable space
					nextFree[j] = i - 1;
				}
			}
		}
		
		AddBall();
		printBoard();
		CheckGameOver();
	}
	
	private void left() {
		// Store the next available space for the ball to move
		int[] nextFree = {0, 0, 0, 0, 0, 0, 0};

		// Set merged array
		bool[,] merged = new bool[7, 7];
		for (int i = 0; i < 7; i++) {
			for (int j = 0; j < 7; j++) {
				merged[i, j] = false;
			}
		}
		
		// Check first column
		for (int i = 0; i < 7; i++) {
			if (board[i, 0] != 0) {
				nextFree[i] = 1;
			}
		}
		
		// Check other rows, move and merge if possible
		for (int j = 1; j < 7; j++) {
			for (int i = 0; i < 7; i++) {
				if (board[i, j] > 0) { // If it is a ball
					if (nextFree[i] > 0 && board[i, j] == board[i, nextFree[i] - 1] && merged[i, nextFree[i] - 1] == false) {
						mergeSprites(i, j, i, nextFree[i] - 1, board[i, j]);
						board[i, nextFree[i] - 1]++;
						merged[i, nextFree[i] - 1] = true;
						board[i, j] = 0;
					} else if (nextFree[i] < j) { // If ball can move that way
						board[i, nextFree[i]] = board[i, j];
						board[i, j] = 0;
						moveSprite(i, j, i, nextFree[i], board[i, nextFree[i]]);
						nextFree[i]++;
					} else {
						nextFree[i]++;
					}
				} else if (board[i, j] < 0) { // -1 == non-usable space
					nextFree[i] = j + 1;
				}
			}
		}
		
		AddBall();
		printBoard();
		CheckGameOver();
	}
	
	private void right() {
		// Store the next available space for the ball to move
		int[] nextFree = {6, 6, 6, 6, 6, 6, 6};

		// Set merged array
		bool[,] merged = new bool[7, 7];
		for (int i = 0; i < 7; i++) {
			for (int j = 0; j < 7; j++) {
				merged[i, j] = false;
			}
		}
		
		// Check first column
		for (int i = 0; i < 7; i++) {
			if (board[i, 6] != 0) {
				nextFree[i] = 5;
			}
		}
		
		// Check other rows, move and merge if possible
		for (int j = 5; j >= 0; j--) {
			for (int i = 0; i < 7; i++) {
				if (board[i, j] > 0) { // If it is a ball
					if (nextFree[i] < 6 && board[i, j] == board[i, nextFree[i] + 1] && merged[i, nextFree[i] + 1] == false) {
						mergeSprites(i, j, i, nextFree[i] + 1, board[i, j]);
						board[i, nextFree[i] + 1]++;
						merged[i, nextFree[i] + 1] = true;
						board[i, j] = 0;
					} else if (nextFree[i] > j) { // If ball can move that way
						board[i, nextFree[i]] = board[i, j];
						board[i, j] = 0;
						moveSprite(i, j, i, nextFree[i], board[i, nextFree[i]]);
						nextFree[i]--;
					} else {
						nextFree[i]--;
					}
				} else if (board[i, j] < 0) { // -1 == non-usable space
					nextFree[i] = j - 1;
				}
			}
		}
		
		AddBall();
		printBoard();
		CheckGameOver();
	}
	
	private void printBoard() {
		for (int i = 0; i < 7; i++) {
			string line = "";
			for (int j = 0; j < 7; j++) {
				if (board[i, j] >= 0 && board[i, j] < 10) { //Single digit number
					line += " ";
				}
				line += board[i, j].ToString();
				line += " ";
			}
			GD.Print(line);
		}
		GD.Print("");
	}
	
	// Returns unless there's a game over
	private void CheckGameOver() {
		
		for (int i = 0; i < 7; i++) {
			for (int j = 0; j < 7; j++) {
				
				// If there's a blank space on the board
				if (board[i, j] == 0) {
					return;
				}
				// Don't apply sameness checks to unusable spaces
				if (board[i, j] > -1) {
					// If space above has the same one
					if (i > 0) {
						if (board[i, j] == board[i-1, j]) {
							return;
						}
					}
					// If space to the left has the same one
					if (j > 0) {
						if (board[i, j] == board[i, j-1]) {
							return;
						}
					}
				}
			}
		}
		
		GD.Print("Game Over!");
		GameOver();
	}
	
	private void GameOver() {
		Sprite target = GetNode<Sprite>("GameOver");
		target.Visible = true;
	}
	
	private void _on_Button_button_up()
	{
		//await Task.Delay(500);
		GetTree().ChangeScene("res://World.tscn");
	}
	
	private void _on_SwipeControls_DownSignal()
	{
		down();
	}


	private void _on_SwipeControls_LeftSignal()
	{
		left();
	}


	private void _on_SwipeControls_RightSignal()
	{
		right();
	}


	private void _on_SwipeControls_UpSignal()
	{
		up();
	}
}
