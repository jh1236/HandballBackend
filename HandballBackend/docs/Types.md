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
    cards: "list[CardStructure]",
    teamOneRating: "int",
    teamTwoRating: "int",
    teamOneNotes: "str",
    teamTwoNotes: "str",
    teamOneProtest: "str",
    teamTwoProtest: "str",
    markedForReview: "bool"
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
  id: "int",
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

## CardStructure Data Structure

```json lines
{
  eventType: "str",
  firstTeam: "bool",
  player: "Player",
  details: "int",
  notes: "str",
}
```

## Person Data Structure

```json lines
{
  name: "str",
  searchableName: "str",
  imageUrl: "str",
  bigImageUrl: "str"
}
```

if admin:

```json lines
{
  isAdmin: "bool",
  gameDetails: "dict[str, {notes: str, rating:  int, cards: list[CardStructure]}"
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
  isBestPlayer: "bool",
  cardTime: "int",
  cardTimeRemaining: "int",
  sideOfCourt: "string",
  isCaptain: "bool",
  startSide: "string",
  stats: {
    "Rounds on Court": "int",
    "Rounds Carded": "int",
    "Points Scored": "int",
    "Aces Scored": "int",
    "Faults": "bool",
    "Double Faults": "int",
    "Served Points": "int",
    "Served Points Won": "int",
    "Serves Received": "int",
    "Serves Returned": "int",
    "Biggest Ace Streak": "int",
    "Biggest Serve Streak": "int",
    "Green Cards": "int",
    "Yellow Cards": "int",
    "Red Cards": "int",
    "Starting Side": "int",
    "Elo": "int",
    "Elo Delta": "int",
  }
}
```

if include_game: (default true)

```json lines
{
  game: "Game",
}
```

if admin:

```json lines
{
  stats: {
    "Rating": "int",
    "Warnings": "int"
  },
}
```

## Team Data Structure

```json lines
{
  name: "str",
  searchableName: "str",
  imageUrl: "str",
  bigImageUrl: "str",
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
  servingFromLeft: "bool",
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

if admin:

```json lines
{
  gameDetails: "dict[str, {notes: str, rating: int, cards: list[CardStructure]}"
}
```

## Tournament Data Structure

```json lines
{
  name: "str",
  searchableName: "str",
  editable: "bool",
  fixturesType: "str",
  finalsType: "str",
  ranked: "bool",
  twoCourts: "bool",
  hasScorer: "bool",
  finished: "bool",
  inFinals: "bool",
  isPooled: "bool",
  notes: "str",
  imageUrl: "srt",
  usingBadmintonServes: "bool",
}
```
