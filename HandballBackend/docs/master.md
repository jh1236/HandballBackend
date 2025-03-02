
# TYPES

## Games Data structure

```json lines
{
    id: "int",
    tournament: "Tournament",
    teamOne: "Team",
    teamTwo: "Team",
    teamOneScore: "int",
    teamTwoScore: "int",
    teamOneTimeouts: "int",
    teamTwoTimeouts: "int",
    firstTeamWinning: "bool",
    started: "bool",
    someoneHasWon: "bool",
    ended: "bool",
    protested: "bool",
    resolved: "bool",
    ranked: "bool",
    bestPlayer: "Person",
    official: "Official",
    scorer: "Official | null",
    firstTeamIga: "bool",
    firstTeamToServe: "bool",
    sideToServe: "str",
    startTime: "float",
    serveTimer: "float",
    length: "float",
    court: "int",
    isFinal: "bool",
    round: "int",
    isBye: "bool",
    status: "str",
    faulted: "bool",
    changeCode: "int",
    timeoutExpirationTime: "float",
    isOfficialTimeout: "bool"
}
```

if admin:

```json lines
{
    admin: {
        noteableStatus: "str",
        notes: "str",
        cards: "list[GameEvents]",
        teamOneRating: "int",
        teamTwoRating: "int",
        teamOneNotes: "str",
        teamTwoNotes: "str",
        teamOneProtest: "str",
        teamTwoProtest: "str"
    }
}
```

if include_game_event: (default False)

```json lines
{
    events: "list[GameEvent<include_game=False>]"
}
```

if include_player_stats: (default False)
- Each team will have its captain and non-captain populated with the relevant `PlayerGameStats` statistics

## GameEvents Data Structure

```json lines
{
    eventType: "str",
    firstTeam: "bool",
    player: "Player",
    details: "int",
    notes: "str",
    firstTeamJustServed: "bool",
    sideServed: "str",
    firstTeamToServe: "bool",
    sideToServe: "str",
    teamOneLeft: "Person",
    teamOneRight: "Person",
    teamTwoLeft: "Person",
    teamTwoRight: "Person",
}
```

if include_game: (default True)

```json lines
{
    game: "Game"
}
```

## Person Data Structure

```json lines
{
    name: "str",
    searchableName: "str",
    imageUrl: "str",
}
```

if admin:

```json lines
{
    isAdmin: "bool"
}
```

if include_stats: (default false)

```json lines
{
    stats: {
        "B&F Votes": "int",
        "Elo": "float",
        "Games Won": "int",
        "Games Lost": "int",
        "Games Played": "int",
        "Percentage": "float",
        "Points Scored": "int",
        "Points Served": "int",
        "Aces Scored": "int",
        "Faults": "int",
        "Double Faults": "int",
        "Green Cards": "int",
        "Yellow Cards": "int",
        "Red Cards": "int",
        "Rounds on Court": "int",
        "Rounds Carded": "int",
        "Net Elo Delta": "float",
        "Average Elo Delta": "float",
        "Points per Game": "float",
        "Points per Loss": "float",
        "Cards": "int",
        "Cards per Game": "float",
        "Points per Card": "float",
        "Serves per Game": "float",
        "Serves per Ace": "float",
        "Serves per Fault": "float",
        "Serve Ace Rate": "float",
        "Serve Fault Rate": "float",
        "Percentage of Points Scored": "float",
        "Percentage of Points Scored For Team": "float",
        "Percentage of Games Starting Left": "float",
        "Percentage of Points Served Won": "float",
        "Serves Received": "int",
        "Serves Returned": "int",
        "Max Serve Streak": "int",
        "Average Serve Streak": "int",
        "Max Ace Streak": "int",
        "Average Ace Streak": "int",
        "Serve Return Rate": "float",
        "Votes per 100 games": "float",
    }
}
```

if include_stats && admin:

```json lines
{
    stats: {
        "Penalty Points": "int",
        "Warnings": "int"
    }
}
```

## Official Data Structure

everything from `Person` plus

```json lines
{
    stats: {
        "Green Cards Given": "int",
        "Yellow Cards Given": "int",
        "Red Cards Given": "int",
        "Cards Given": "int",
        "Cards Per Game": "float",
        "Faults Called": "int",
        "Faults Per Game": "float",
        "Games Umpired": "int",
        "Games Scored": "int",
        "Rounds Umpired": "int",
    }
}
```

## PlayerGameStats Data Structure

everything from `Person` plus

```json lines
{
    team: "Team",
    roundsOnCourt: "int",
    roundsCarded: "int",
    pointsScored: "int",
    acesScored: "int",
    isBestPlayer: "bool",
    faults: "int",
    doubleFaults: "int",
    servedPoints: "int",
    servedPointsWon: "int",
    servesReceived: "int",
    servesReturned: "int",
    aceStreak: "int",
    serveStreak: "int",
    warnings: "int",
    greenCards: "int",
    yellowCards: "int",
    redCards: "int",
    cardTime: "int",
    cardTimeRemaining: "int",
    startSide: "str",
    elo: "float",
    eloDelta: "float",
    sideOfCourt: "str",
    isCaptain: "bool"
}
```

if include_game: (default true)

```json lines
{
    game: "Game",
}
```

## Team Data Structure

```json lines
{
    name: "str",
    searchableName: "str",
    imageUrl: "str",
    captain: "Person",
    nonCaptain: "Person | PlayerGameStats | null",
    substitute: "Person | PlayerGameStats | null",
    teamColor: "str",
    elo: "float"
}
```

if game_id:
```json lines
{
    servedFromLeft: "bool",
    eloDelta: "float"
}
```

if include_stats: (default false)

```json lines
{
    stats: {
        "Elo": "float",
        "Games Played": "int",
        "Games Won": "int",
        "Games Lost": "int",
        "Percentage": "float",
        "Green Cards": "int",
        "Yellow Cards": "int",
        "Red Cards": "int",
        "Faults": "int",
        "Double Faults": "int",
        "Timeouts Called": "int",
        "Points Scored": "int",
        "Points Against": "int",
        "Point Difference": "int",
    }
}
```

## Tournament Data Structure

```json lines
{
    name: "str",
    searchableName: "str",
    editable: "bool",
    fixturesType:  "str",
    finalsType: "str",
    ranked:  "bool",
    twoCourts: "bool",
    hasScorer:  "bool",
    finished: "bool",
    inFinals:  "bool",
    isPooled: "bool",
    notes:  "str",
    imageUrl: "srt",
    usingBadmintonServes:  "bool",
}
```

# GAMES API REFERENCE

## GET endpoints

### /api/games/change_code

#### Description

Returns the id of the most recent GameEvent for a game. Used to know if a game has an update

#### Permissions:

The user must be logged in as an official to use this endpoint

#### Arguments:

- id: int
    - The id of the game

#### Return Structure

- code: int = The id of the most recent update to the game

<hr>

### /api/games/<id>

#### Description

Returns the data for a single game

#### Permissions:

This endpoint is open to the public

#### Arguments:

- id: int
    - The id of the game
- includeGameEvents: bool
    - True if the events of the game should be included
- includeStats
    - True if the stats of each player should be included
- includePreviousCards: bool (official only)
    - True if the cards each team has previously received should be included

#### Return Structure

- game: Game(includeGameEvents=includeGameEvents)

<hr>

### /api/games

#### Description

Returns the list of games matching the given criteria

#### Permissions:

This endpoint is open to the public

#### Arguments:

- tournament: str (Optional)
    - The searchable name of the tournament to take games from
- team: str (Optional) (Repeatable)
    - The searchable name of the team who played in the game
- player: str (Optional) (Repeatable)
    - The searchable name of the player who played in the game
- official: str (Optional) (Repeatable)
    - The searchable name of the official who umpired or scored the game
- court
    - The court the game was played on
- includeGameEvents (bool) (Optional) (**admin** only)
    - True if the events of the games should be included
- includePlayerStats (bool) (Optional)(**admin** only)
    - True if the stats of each player should be included
- returnTournament: bool (Optional)
    - If the tournament is to be returned in the response

#### Return Structure

- games: list\[Game(includeGameEvents=includeGameEvents)\]
- tournament: Tournament
    - The tournament that was passed in

<hr>

### /api/games/fixtures

#### Description

Returns the fixtures for a given tournament

#### Permissions:

This endpoint is open to the public

#### Arguments:

- tournament: str (Optional)
    - The searchable name of the tournament to take games from
- team: str (Optional) (Repeatable)
    - The searchable name of the team who played in the game
- player: str (Optional) (Repeatable)
    - The searchable name of the player who played in the game
- official: str (Optional) (Repeatable)
    - The searchable name of the official who umpired or scored the game
- court
    - The court the game was played on
- includeGameEvents
    - True if the events of the games should be included
- includePlayerStats
    - True if the stats of each player should be included
- returnTournament: bool (Optional)
    - If the tournament is to be returned in the response
- separateFinals
    - True if the finals should be returned in a separate list

#### Return Structure

- fixtures: list
    - games: list\[Games\]
    - final: boolean
- finals: list\[Game(includeGameEvents=includeGameEvents)\]?
    - games: list\[Games\]
    - final: true
- tournament: Tournament
    - The tournament that was passed in

<hr>
<hr>

## POST endpoints

### /api/games/update/start

#### Description

Starts a game in the system, setting the side of the court and the team who is serving first.

#### Permissions:

The user must be logged in as an official to use this endpoint

#### Arguments:

- id: int
    - The id of the game
- swapService: bool
    - True if the team listed first is not serving
- teamOneIGA: bool
    - True if the team listed first is on the IGA side of the court
- teamOne: list\[str\]
    - The searchable names of each player on team one, in the order \[Left Player, Right Player, Substitute\]
- teamTwo: list\[str\]
    - The searchable names of each player on team two, in the order \[Left Player, Right Player, Substitute\]
- official: str (Optional)
    - the searchable name of the person who is officiating the game
- scorer: str (Optional)
    - the searchable name of the person who is scoring the game

#### Return Structure

- N/A

<hr>

### /api/games/update/score

#### Description

Adds one to the score of a team.

#### Permissions:

The user must be logged in as an official to use this endpoint

#### Arguments:

- id: int
    - The id of the game
- firstTeam: bool
    - True if the team listed first scored the point
- leftPlayer: bool
    - True if the player who started the point on the left side scored the point
- method: 'Double Bounce' | 'Straight' | 'Out of Court' | 'Double Touch' | 'Grabs' | 'Illegal Body Part'| 'Obstruction'
    - The way that the point was scored

#### Return Structure

<hr>

- N/A

### /api/games/update/ace

#### Permissions:

Adds one to the score of a team, and adds an ace statistic to the relevant player

#### Arguments:

- id: int
    - The id of the game

#### Return Structure

- N/A

<hr>

### /api/games/update/substitute

#### Description

Used to swap a substitute onto the court.

#### Permissions:

The user must be logged in as an official to use this endpoint

#### Arguments:

- id: int
    - The id of the game
- firstTeam: bool
    - True if the team listed first is substituting
- leftPlayer: bool
    - True if the player who is currently on the left side is going to substitute

#### Return Structure

- N/A

<hr>

### /api/games/update/pardon

#### Description

Allows a player to return to the court early from being carded

#### Permissions:

The user must be logged in as an **admin** to use this endpoint

#### Arguments:

- id: int
    - The id of the game
- firstTeam: bool
    - True if the team listed first is substituting
- leftPlayer: bool
    - True if the player who is currently on the left side is going to substitute

#### Return Structure

- N/A

<hr>

### /api/games/update/resolve

#### Description

Marks the game as resolved, clearing the notification that it needs to be processed

#### Permissions:

The user must be logged in as an **admin** to use this endpoint

#### Arguments:

- id: int
    - The id of the game

#### Return Structure

- N/A

<hr>

### /api/games/update/end

#### Description

Ends a game, setting a fairest & best and taking notes and protests.

#### Permissions:

The user must be logged in as an official to use this endpoint

#### Arguments:

- id: int
    - The id of the game
- bestPlayer: str
    - The searchable name of the player who played best
- notes: str (Optional)
    - Any notes that the umpire would like to leave for the tournament director
- protestTeamOne: str (Optional)
    - If present, represents the reason that team one wants to protest
- protestTeamTwo: str (Optional)
    - If present, represents the reason that team two wants to protest
- notesTeamOne: str (Optional)
    - If present, represents the notes the umpire left for team one
- notesTeamTwo: str (Optional)
    - If present, represents the notes the umpire left for team two
- markedForReview: bool (Optional)
    - If the game needs to be reviewed by a supervisor

#### Return Structure

- N/A

<hr>


### /api/games/update/forfeit

#### Description

Forfeits the game for a team, setting their opponent's score
to either 11 or 2 plus their score, whichever is greater.

#### Permissions:

The user must be logged in as an official to use this endpoint

#### Arguments:

- id: int
    - The id of the game
- firstTeam: bool
    - True if the team listed first is forfeiting

#### Return Structure

- N/A

<hr>

### /api/games/update/timeout

#### Description

Starts a timeout for a given team

#### Permissions:

The user must be logged in as an official to use this endpoint

#### Arguments:

- id: int
    - The id of the game
- firstTeam: bool
    - True if the team listed first called the timeout

#### Return Structure

- N/A

<hr>

### /api/games/update/endTimeout

#### Description

Ends the current timeout for a game

#### Permissions:

The user must be logged in as an official to use this endpoint

#### Arguments:

- id: int
    - The id of the game

#### Return Structure

- N/A

<hr>

### /api/games/update/serveClock

#### Description

Either starts or ends the serve clock.

#### Permissions:

The user must be logged in as an official to use this endpoint

#### Arguments:

- id: int
    - The id of the game
- start: bool
    - True if the clock is being started, False if the clock is being ended

#### Return Structure

- N/A

<hr>

### /api/games/update/fault

#### Description

Adds a fault to the current server, if this is their second fault, gives a point to the other team.

#### Permissions:

The user must be logged in as an official to use this endpoint

#### Arguments:

- id: int
    - The id of the game

#### Return Structure

- N/A

<hr>

### /api/games/update/officialTimeout

#### Description

Starts an official timeout.

#### Permissions:

The user must be logged in as an official to use this endpoint

#### Arguments:

- id: int
    - The id of the game

#### Return Structure

- N/A

<hr>

### /api/games/update/undo

#### Description

Undoes the last game event, unless the previous event was an end timeout, in which case it undoes the last 2 events.

#### Permissions:

The user must be logged in as an official to use this endpoint.
If the game has ended, the user must be logged in as an **admin** to use this endpoint.

#### Arguments:

- id: int
    - The id of the game

#### Return Structure

- N/A

<hr>

### /api/games/update/delete

#### Description

Deletes a game.

#### Permissions:

The user must be logged in as an official to use this endpoint.
If the game has started, the user must be logged in as an **admin** to use this endpoint.

#### Arguments:

- id: int
    - The id of the game

#### Return Structure

- N/A

<hr>

### /api/games/update/card

#### Description

Cards a player in a game.

#### Permissions:

The user must be logged in as an official to use this endpoint.

#### Arguments:

- id: int
    - The id of the game
- firstTeam: bool
    - True if the team listed first is receiving the card
- leftPlayer: bool
    - True if the player who is currently on the left side is receiving the card
- color: "Warning" | "Green" | "Yellow" | "Red"
    - the type of card that the player has been issued
- duration: int
    - the amount of rounds the player is off for (-1 for the rest of the game)
- reason: str
    - the reason that the card was given

#### Return Structure

- N/A

<hr>

### /api/games/update/create

#### Description

Creates a new game.

#### Permissions:

The user must be logged in as an official to use this endpoint.

#### Arguments:

- tournament: str
    - The searchable name of the tournament for the new game
- teamOne: str
    - The searchable name of team one, or the new name for team one if it is new
- teamTwo: str
    - The searchable name of team two, or the new name for team one if it is new
- official: str
    - the searchable name of the umpire for the gane
- scorer: str (Optional)
    - the searchable name of the scorer for the gane
- playersOne: list\[str\] (Optional)
    - a list of the **true names** of the players in team one. Any new players will be implicitly created
- playersTwo: list\[str\] (Optional)
    - a list of the **true names** of the players in team two. Any new players will be implicitly created

#### Return Structure

- N/A

<hr>

# TEAMS API REFERENCE

## GET endpoints

### /api/teams

#### Description

Returns all teams, adjusted for a tournament if passed in

#### Permissions:

This endpoint is open to the public.

#### Arguments:

- tournament: str (Optional)
    - The searchable name of the tournament to get officials from
- player: str (Optional)
    - The searchable name of the player that must be in the team
- includeStats: bool (Optional)
    - True if the stats of each team should be included
- formatData: bool (Optional)
    - True if the server should format the data before it is sent.
- includePlayerStats: bool (Optional)
    - if the stats for each player should be included
- returnTournament: bool (Optional)
    - If the tournament is to be returned in the response

#### Return Structure

- teams: list\[Team\]
- tournament: Tournament
    - The tournament that was passed in

<hr>

### /api/teams/<searchable>

#### Description

Returns the details of a single team, with the option to filter stats from a certain tournament.

#### Permissions:

This endpoint is open to the public.

#### Arguments:

- searchable: str
    - The searchable name of the team to get stats for
- tournament: str (Optional)
    - The searchable name of the tournament to get stats from
- formatData: bool (Optional)
    - True if the server should format the data before it is sent.
- returnTournament: bool (Optional)
    - If the tournament is to be returned in the response

#### Return Structure

- Team

<hr>

### /api/ladder

#### Description

Returns the ladder for a given tournament.

#### Permissions:

This endpoint is open to the public.

#### Arguments:

- tournament: str (Optional)
    - The searchable name of the team to get the ladder for
- includeStats: bool (Optional)
    - The searchable name of the tournament to get stats from
- formatData: bool (Optional)
    - True if the server should format the data before it is sent.
- returnTournament: bool (Optional)
    - If the tournament is to be returned in the response

#### Return Structure

- pooled: bool
    - True if the data is pooled
- ladder: list\[Team\]
    - The list of teams in order if the tournament is _not_ pooled
- poolOne: list\[Team\]
    - The list of teams in order in pool 1 if the tournament is pooled
- poolTwo: list\[Team\]
    - The list of teams in order in pool 2 if the tournament is pooled
- tournament: Tournament
    - The tournament that was passed in

<hr>

### /api/teams/image

#### Description

Returns the image for a given tournament.

#### Permissions:

This endpoint is open to the public.

#### Arguments:

- name: str (Optional)
    - The searchable name of the team to get an image from

#### Return Structure

- image

<hr>
# PLAYERS API REFERENCE

## GET endpoints

### /api/players

#### Description

Returns all players, adjusted for a tournament if passed in.

#### Permissions:

This endpoint is open to the public.

#### Arguments:

- tournament: str (Optional)
    - The searchable name of the tournament to get officials from
- team: str (Optional)
    - The searchable name of the team that the players play on
- includeStats: bool (Optional)
    - True if the stats of each player should be included
- formatData: bool (Optional)
    - True if the server should format the data before it is sent.
- returnTournament: bool (Optional)
    - If the tournament is to be returned in the response

#### Return Structure

- players: list\[Person\]
- tournament: Tournament
    - The tournament that was passed in

<hr>

### /api/players/<searchable>

#### Description

Returns the details of a single player, with the option to filter stats from a certain tournament
or game. If the game is passed, a PlayerGameStats is returned, otherwise a Player is returned

#### Permissions:

This endpoint is open to the public.

#### Arguments:

- searchable: str
    - The searchable name of the player to get stats for
- tournament: str (Optional)
    - The searchable name of the tournament to get stats from
- game: int (Optional)
    - The id of the game that was played
- includeCourtStats: bool (Optional)
    - True if the stats of the player on each court are to be included.
- formatData: bool (Optional)
    - True if the server should format the data before it is sent.
- returnTournament: bool (Optional)
    - If the tournament is to be returned in the response

#### Return Structure

- player: Person | PlayerGameStats   (See description)
- tournament: Tournament
    - The tournament that was passed in

<hr>

### /api/users/image

#### Description

Returns the image for a given user.

#### Permissions:

This endpoint is open to the public.

#### Arguments:

- name: str
    - The searchable name of the user to get an image from

#### Return Structure

- image

<hr>

### /api/players/stats

#### Description

Returns the average stats over a given tournament

#### Permissions:

This endpoint is open to the public.

#### Arguments:

- tournament: str (Optional)
    - The searchable name of the tournament to get officials from
- game: int (Optional)
    - The id of the game to get average stats for
- formatData: bool (Optional)
    - True if the server should format the data before it is sent.
- returnTournament: bool (Optional)
    - If the tournament is to be returned in the response

#### Return Structure

- players: list\[Person\]
- tournament: Tournament
    - The tournament that was passed in

<hr>
# TOURNAMENTS API REFERENCE

## GET endpoints

### /api/tournaments/<searchable>

#### Description

Returns a single Tournament

#### Permissions:

This endpoint is open to the public.

#### Arguments:

- searchable: str
    - The searchable name of the tournament to get stats for

#### Return Structure

- tournament: Tournament

<hr>

### /api/tournaments/

#### Description

Returns all Tournaments

#### Permissions:

This endpoint is open to the public.

#### Arguments:

- N/A

#### Return Structure

- tournaments: list\[Tournament\]

<hr>

### /api/tournaments/image

#### Description

Returns the image for a given tournament.

#### Permissions:

This endpoint is open to the public.

#### Arguments:

- tournament: str (Optional)
    - The searchable name of the tournament to get an image from

#### Return Structure

- image

<hr>

### /api/tournaments/\<tournament\>/winners

#### Description

Returns the image for a given tournament.

#### Permissions:

This endpoint is open to the public.

#### Arguments:

- tournament: str
    - The searchable name of the tournament to get the winners for

#### Return Structure

- first: Team
  - the team that came in first
- second: Team
  - the team that came in second
- third: Team
  - the team that came in third
- podium: List\[Team\]
  - a list of the top 3 teams in order 

<hr>
<hr>

## POST endpoints

### /api/tournaments/notes

#### Description

Sets the note for a given tournament.

#### Permissions:

The user must be logged in as an **admin** to use this endpoint

#### Arguments:

- tournament: str
    - The searchable name of the tournament to set the note for

- note: str
    - the note for the front page of the tournament

#### Return Structure

- N/A

<hr>

### /api/tournaments/serveStyle

#### Description

Changes the serving style for a tournament.

#### Permissions:

The user must be logged in as an **admin** to use this endpoint

#### Arguments:

- tournament: str
    - The searchable name of the tournament to set the note for
- badmintonServes: bool (Optional)
    - True if the tournament should use badminton serves. If omitted, it will toggle the state of the badminton serve
      data

#### Return Structure

- N/A

<hr>

# OFFICIALS API REFERENCE

## GET endpoints

### /api/officials

#### Description

Returns all officials and their stats, adjusted for a tournament if passed in

#### Permissions:

This endpoint is open to the public.

#### Arguments:

- tournament: str (Optional)
    - The searchable name of the tournament to get officials from
- returnTournament: bool (Optional)
    - If the tournament is to be returned in the response

#### Return Structure

- officials: Official
- tournament: Tournament
  - The tournament that was passed in
<hr>

### /api/officials/<searchable>

#### Description

Returns the details of a single official, with the option to filter stats from a certain tournament

#### Permissions:

This endpoint is open to the public.

#### Arguments:

- tournament: str (Optional)
    - The searchable name of the tournament to get stats from
- returnTournament: bool (Optional)
    - If the tournament is to be returned in the response
#### Return Structure

- Official
- tournament: Tournament
  - The tournament that was passed in
<hr>

# USERS API REFERENCE

## POST endpoints


### /api/login

#### Description

Logs in a user

#### Permissions:

This endpoint is open to the public.

#### Arguments:

- userId: int
  - the ID of the user attempting to log in
- password: str
  - the password of the user attempting to log in

#### Return Structure

- token: str
  - the token that the user received


### /api/image/

#### Description

Changes the users image to be the image at a given URL.

#### Permissions:

The user must be logged in as an official to use this endpoint.

#### Arguments:

- imageLocation: url
    - The URL of an image.

#### Return Structure

- N/A

<hr>
