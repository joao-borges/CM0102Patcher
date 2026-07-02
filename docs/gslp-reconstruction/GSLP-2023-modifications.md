# GSLP 2023 — Complete Modification List (reference)

Scraped 2026-07-02 from https://cm0102brasil.wixsite.com/cmbr/modificacoes-gs-leagues-patch (translated).
Author: Giovani Santana ("GS"). Built by trial & error in OllyDbg on top of the 2023 champman-forum
patch stack (no source or change log exists — this list + binary diffing is the reconstruction basis).

**Key insight:** almost every "new" competition REPURPOSES an existing one (structure hijacking),
which is what makes reconstruction and porting feasible. Donor competitions are noted per item.

## Brazil
- Série A and B with 20 clubs, round-robin (donor: Danish league divisions; Denmark removed)
- Série C with 20 clubs, knockout format
- Série D added, 36 teams, round-robin
- Copa do Brasil with Libertadores qualifiers, February–September
- Copa do Nordeste added (donor: Danish Cup)
- Supercopa do Brasil added (donor: Welsh Cup)
- State championships modified to avoid calendar congestion
- Campeonato Paulista in current-like format (no group stage)
- Central and Northern state leagues expanded to 18 teams
- Foreign player limit increased to 7
- Two transfer windows

## Argentina
- New 2nd division without errors (donor: Greek 2nd division; Greece becomes single-division)
- Copa Argentina created (donor: Irish Cup Challenge)

## Mexico
- Mexican League added (donor: Australian league)
- 2nd division added (donor: Scottish 4th division)

## United States
- New MLS with 26 clubs (donor: original Brazilian 1st division, knockout format)

## Japan
- Emperor's Cup and League Cup dates matched to reality

## Italy
- Coppa Italia modern format (donor: Polish League Cup)
- "Spareggio" playoff eliminated

## Germany
- German Super Cup added (donor: old Intercontinental Cup)
- German clubs' transfer fund limiter removed

## France
- Stability improvements (calendar changes, promotion code alterations)
- Extra-community limit: 4 in D1–D2, 3 in D3
- Cotonou Agreement partially applied (African players not extra-community)

## Spain
- La Liga calendar slightly delayed (post-season date fix)
- Copa del Rey single matches (except semifinals)

## South Africa
- South African League added (donor: Northern Irish league)

## South America
- Libertadores runs February–November
- Copa Sudamericana added (donor: CONCACAF Champions League)
- Libertadores and Sudamericana finals single-match
- Recopa Sudamericana added (donor: extinct Inter-American Cup)
- CONMEBOL competitions without away-goals rule
- Copa América in even years (2020, 2024, 2028…)
- Libertadores slots: Brazil/Argentina 5, Chile/Colombia 4, others 2

## North America
- New CONCACAF Champions League (donor: extinct Asian Recopa)
- CONCACAF Gold Cup created (donor: original Club World Cup)

## Europe
- 30 players allowed in UEFA competitions
- Intertoto renamed UEFA Conference League
- Euros one week later (Champions League final conflict fix)
- UEFA competitions without away-goals rule
- January transfers between Champions League clubs permitted

## Asia
- Asian Champions League dates/formats adjusted
- Japanese and South Korean champions/runners-up participate without errors

## Africa
- African Champions League created (donor: extinct Merconorte Cup)
- African Nations Cup in odd years

## World
- Club World Cup 2023-like format, excludes host nation, semifinal draw allows
  intercontinental matchups (donor: German League Cup)

## Other modifications
- All financial values inflated 1.5x; player transfer values slightly increased
- Brazilian Real adjusted (£1 = R$5)
- Five-substitution rule
- Fixed Reflexes/Creativity attribute bug for high-CA players (a.k.a. UnlockYourCreativity)
- Confederations Cup disabled; host nations modified; Australia in AFC
- B-Teams deactivated
- Unprotected contracts disabled "naturally"; month-to-month contracts unavailable
- Player age visibility; AI player contract status visible; status changeable during offers
- Tours functional in updated and original databases
- Brazilian/South American prize money made realistic
- Graphics, game text and narration modified
- Additional transfer proposal defaults
- Work permit easier for extra-community players in UK
- U20/U21 caps no longer prevent nationality changes
- Trial periods for free agents removed
- Can apply to clubs in countries without loaded leagues
- Players keep fitness during holidays; physical attributes decline slower with age
- More players loaded from clubs without leagues
- Renewal-proposal player cannot be switched; young players accept proposals normally
- Board won't cancel transfers as unrealistic
- Dual-nationality years: Spain/Portugal 3, UK/Ireland 5, China/Finland/Russia 7, others 6

## New qualifiers
- Copa Sudamericana: Nordeste Cup champion + Copa do Brasil runner-up
- Copa do Nordeste: all teams from Bahia and Pernambuco state championships
- CONCACAF CL: Mexican champion/runner-up, MLS champion, US Open Cup champion
- African CL: South African 1st Division champion/runner-up

## Known issues (author-acknowledged)
- Asian CL lacks South Korean teams unless that league is loaded
- CONCACAF CL lacks Mexican teams unless that league is loaded
- CONCACAF CL appears in the Asian competition list
- Argentine Sudamericana winner's Libertadores spot incorrectly goes to Brazil
