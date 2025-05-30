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