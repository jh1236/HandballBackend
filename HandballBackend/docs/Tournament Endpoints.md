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
