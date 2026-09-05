# Sunrise added start — random content variants for character record randomize buttons
# Composite templates (emergency-contact/arrest/imprisonment/employment-history/residence-street)
# are assembled by the generator from independent pieces, so the real number of
# combinations is much larger than the line count below.

records-random-apartment-unit = Unit {$value}

## Emergency contact: name (generated separately) + relation + contact method
records-random-emergency-relation-1 = mother
records-random-emergency-relation-2 = father
records-random-emergency-relation-3 = spouse
records-random-emergency-relation-4 = brother
records-random-emergency-relation-5 = sister
records-random-emergency-relation-6 = legal guardian
records-random-emergency-method-1 = station comms channel
records-random-emergency-method-2 = personal transceiver
records-random-emergency-method-3 = backup channel via dispatch
records-random-emergency-method-4 = standard NanoTrasen emergency channel
records-random-emergency-code = service comms code {$code}
records-random-emergency-contact-template = {$name} ({$relation}), {$method}
records-random-emergency-official-1 = NanoTrasen duty representative at home posting
records-random-emergency-official-2 = No emergency contact listed, refer to station administration

records-random-close-relatives-1 = Parents reside on orbital station NT-Ceres
records-random-close-relatives-2 = Only child, no close relatives on file
records-random-close-relatives-3 = Older sister serves in another NanoTrasen sector
records-random-close-relatives-4 = Parents are civilian specialists at home posting
records-random-close-relatives-5 = Spouse and one minor child
records-random-close-relatives-6 = Younger brother attends a colonial university
records-random-close-relatives-7 = Relatives live apart, contact is infrequent
records-random-close-relatives-8 = Legal guardian resides at home posting
records-random-close-relatives-9 = Cousin works for the same corporation in a neighboring sector
records-random-close-relatives-10 = No relatives found in the NanoTrasen database
records-random-close-relatives-11 = Grandparents reside at a veterans' care facility at home posting

records-random-notes-1 = No notable remarks on file
records-random-notes-2 = Data requires verification at next visit
records-random-notes-3 = Information provided by the subject, unverified
records-random-notes-4 = Entry generated automatically, pending review
records-random-notes-5 = No additional comments
records-random-notes-6 = Some information is outdated and awaiting confirmation
records-random-notes-7 = Entry carried over from a previous posting's archive
records-random-notes-8 = Entry pending confirmation from the subject's direct supervisor
records-random-notes-9 = Some fields filled in based on third-party statements
records-random-notes-10 = Dossier reviewed following the last personnel rotation
records-random-notes-11 = No issues found with the record's formatting

records-random-postmortem-1 = Cremation, ashes returned to family
records-random-postmortem-2 = Organ donation consent, absent contraindications
records-random-postmortem-3 = Body return to home posting for family burial
records-random-postmortem-4 = Cryostorage pending arrival of next of kin
records-random-postmortem-5 = Standard station procedure, no special instructions
records-random-postmortem-6 = Burial per home planet customs
records-random-postmortem-7 = Decision pending confirmation from family
records-random-postmortem-8 = Burial in open space per the deceased's wishes
records-random-postmortem-9 = Remains handed over to the station's corporate chaplain
records-random-postmortem-10 = Body donated for medical faculty research
records-random-postmortem-11 = No instructions on file, decision left to station command

records-random-physiological-1 = Mild intolerance to standard rations, diet adjusted
records-random-physiological-2 = No chronic conditions detected
records-random-physiological-3 = Lowered pain threshold, noted at last checkup
records-random-physiological-4 = Allergic reaction to certain anesthetics
records-random-physiological-5 = Readings within normal range for species and age
records-random-physiological-6 = Above-average stamina, noted at hiring checkup
records-random-physiological-7 = Follow-up examination recommended per last checkup
records-random-physiological-8 = Minor blood pressure deviation, no correction required
records-random-physiological-9 = Increased sensitivity to gravity fluctuations
records-random-physiological-10 = Chronic fatigue managed with standard station medication
records-random-physiological-11 = Metabolic quirks noted, diet not adjusted

records-random-psychological-1 = No signs of disorder detected
records-random-psychological-2 = Mild anxiety in confined spaces
records-random-psychological-3 = Undergoing routine station psychologist checkups
records-random-psychological-4 = Stress-resilient, no deviations recorded
records-random-psychological-5 = Increased irritability under stress noted
records-random-psychological-6 = Mild insomnia attributed to workload
records-random-psychological-7 = Monitoring recommended following an incident at previous posting
records-random-psychological-8 = Mild attention lapses during extended isolation
records-random-psychological-9 = Increased anxiety before EVA operations
records-random-psychological-10 = Attending voluntary psychological support sessions
records-random-psychological-11 = No deviations found at the last evaluation

records-random-residence-region-1 = Aurora Sector
records-random-residence-region-2 = Outer Belt
records-random-residence-region-3 = Central Cluster
records-random-residence-region-4 = Fringe Sector
records-random-residence-region-5 = New Dawn Sector
records-random-residence-region-6 = Cassiopeia Sector
records-random-residence-region-7 = Border Cluster
records-random-residence-region-8 = Perseus Sector
records-random-residence-region-9 = Inner Ring
records-random-residence-region-10 = Möbius Sector
records-random-residence-region-11 = Far Frontier

records-random-residence-planet-1 = Mars, Tharsis Dome
records-random-residence-planet-2 = Luna, Kuiper Base
records-random-residence-planet-3 = Earth, orbital station
records-random-residence-planet-4 = New Delphi Colony
records-random-residence-planet-5 = NanoTrasen Station "Sunrise"
records-random-residence-planet-6 = Titan, domed settlement
records-random-residence-planet-7 = Ganymede, research outpost
records-random-residence-planet-8 = Europa, sub-ice station
records-random-residence-planet-9 = Ceres, mining colony
records-random-residence-planet-10 = Venus, atmospheric platform
records-random-residence-planet-11 = Pluto, deep-survey outpost

records-random-residence-details-1 = Shared with contract housemates
records-random-residence-details-2 = Private housing module
records-random-residence-details-3 = Short-term lease housing
records-random-residence-details-4 = NanoTrasen service housing
records-random-residence-details-5 = No permanent registration on file
records-random-residence-details-6 = Housing provided by the receiving department
records-random-residence-details-7 = Shared residence with spouse
records-random-residence-details-8 = Residing in an educational institution's dormitory
records-random-residence-details-9 = Property owned under a long-term mortgage
records-random-residence-details-10 = Temporary placement in the station's guest module
records-random-residence-details-11 = Shared residence with fellow crew outside of contract

## Street: base name (no number) + procedurally generated block/unit number
records-random-residence-street-name-1 = Housing block
records-random-residence-street-name-2 = Engineer's Avenue
records-random-residence-street-name-3 = "Gamma" housing module sector
records-random-residence-street-name-4 = Pioneer Street
records-random-residence-street-name-5 = NT dormitory block
records-random-residence-street-template = {$name}, {$number}

records-random-identifying-features-1 = Small scar above left eyebrow
records-random-identifying-features-2 = Forearm tattoo with station insignia
records-random-identifying-features-3 = Noticeable accent typical of home region
records-random-identifying-features-4 = No distinguishing marks on file
records-random-identifying-features-5 = Scar on the back of the hand
records-random-identifying-features-6 = Piercing, removed per station security policy
records-random-identifying-features-7 = Distinctive gait from an old injury
records-random-identifying-features-8 = Birthmark on the neck, hidden by the uniform collar
records-random-identifying-features-9 = Missing part of a finger on the left hand
records-random-identifying-features-10 = Heterochromia, noted at intake examination
records-random-identifying-features-11 = Noticeable forearm scar from a workplace injury

## Arrest history: either a clean record OR an incident tree (outcome -> reason -> optional detail,
## each reason only exists under its own outcome, not in a shared pool — combinations always make sense)
records-random-arrest-clean-1 = No detentions on file
records-random-arrest-clean-2 = Information sealed by management decision
records-random-arrest-clean-3 = No convictions or citations on file
records-random-arrest-clean-4 = No detention records found in the station database
records-random-arrest-clean-5 = Background check found no violations
records-random-arrest-outcome-1 = One administrative detention
records-random-arrest-outcome-1-reason-1 = for an access control violation
records-random-arrest-outcome-1-reason-1-suffix-1 = , case closed without further action
records-random-arrest-outcome-1-reason-1-suffix-2 = , noted in personnel file
records-random-arrest-outcome-1-reason-2 = for minor disorderly conduct
records-random-arrest-outcome-1-reason-2-suffix-1 = , a verbal reprimand was issued
records-random-arrest-outcome-1-reason-2-suffix-2 = , a fine was issued
records-random-arrest-outcome-1-reason-3 = for smoking in a restricted area
records-random-arrest-outcome-1-reason-3-suffix-1 = , a fine was issued
records-random-arrest-outcome-1-reason-3-suffix-2 = , a verbal warning was given instead
records-random-arrest-outcome-1-reason-4 = for violating quiet hours in the residential sector
records-random-arrest-outcome-1-reason-4-suffix-1 = , the dispute was resolved on the spot
records-random-arrest-outcome-1-reason-4-suffix-2 = , neighbors filed a repeat complaint
records-random-arrest-outcome-2 = A formal warning from station security on record
records-random-arrest-outcome-2-reason-1 = for violating internal station regulations
records-random-arrest-outcome-2-reason-1-suffix-1 = , no repeat violations recorded
records-random-arrest-outcome-2-reason-1-suffix-2 = , placed under additional monitoring
records-random-arrest-outcome-2-reason-2 = for improper handling of station equipment
records-random-arrest-outcome-2-reason-2-suffix-1 = , additional briefing conducted
records-random-arrest-outcome-2-reason-2-suffix-2 = , no further consequences
records-random-arrest-outcome-2-reason-3 = for entering a restricted work area without clearance
records-random-arrest-outcome-2-reason-3-suffix-1 = , clearance temporarily restricted
records-random-arrest-outcome-2-reason-3-suffix-2 = , access permissions reviewed again
records-random-arrest-outcome-2-reason-4 = for failing to follow airlock protocol
records-random-arrest-outcome-2-reason-4-suffix-1 = , additional safety briefing assigned
records-random-arrest-outcome-2-reason-4-suffix-2 = , incident recorded with no further action
records-random-arrest-outcome-3 = Brief questioning over a station incident
records-random-arrest-outcome-3-reason-1 = as a witness to the incident
records-random-arrest-outcome-3-reason-1-suffix-1 = , statement entered into the case file
records-random-arrest-outcome-3-reason-1-suffix-2 = , no complaints filed
records-random-arrest-outcome-3-reason-2 = on a mistaken suspicion, later cleared
records-random-arrest-outcome-3-reason-2-suffix-1 = , incident closed without consequences
records-random-arrest-outcome-3-reason-2-suffix-2 = , a formal apology was issued
records-random-arrest-outcome-3-reason-3 = due to a resemblance to a wanted individual
records-random-arrest-outcome-3-reason-3-suffix-1 = , identity confirmed, matter resolved
records-random-arrest-outcome-3-reason-3-suffix-2 = , an apology was issued for the inconvenience
records-random-arrest-outcome-3-reason-4 = following an anonymous tip that was not substantiated
records-random-arrest-outcome-3-reason-4-suffix-1 = , the reporting party could not be identified
records-random-arrest-outcome-3-reason-4-suffix-2 = , case closed for lack of grounds
records-random-arrest-outcome-4 = Detention during a routine inspection
records-random-arrest-outcome-4-reason-1 = due to an expired access permit
records-random-arrest-outcome-4-reason-1-suffix-1 = , permit renewed on the spot
records-random-arrest-outcome-4-reason-1-suffix-2 = , reissued within a day
records-random-arrest-outcome-4-reason-2 = following a random document check
records-random-arrest-outcome-4-reason-2-suffix-1 = , no violations found
records-random-arrest-outcome-4-reason-2-suffix-2 = , a warning was issued
records-random-arrest-outcome-4-reason-3 = during an unscheduled security sweep
records-random-arrest-outcome-4-reason-3-suffix-1 = , no violations found
records-random-arrest-outcome-4-reason-3-suffix-2 = , records forwarded to the station archive
records-random-arrest-outcome-4-reason-4 = at the request of an adjacent station department
records-random-arrest-outcome-4-reason-4-suffix-1 = , the request was later withdrawn
records-random-arrest-outcome-4-reason-4-suffix-2 = , the procedure was completed as usual
records-random-arrest-template = {$outcome} {$reason}{$suffix}

## Imprisonment history: same tree scheme as arrest history
records-random-imprisonment-clean-1 = No prior incarceration on file
records-random-imprisonment-clean-2 = Data unavailable in open records
records-random-imprisonment-clean-3 = No prior convictions
records-random-imprisonment-clean-4 = No record of incarceration outside the station
records-random-imprisonment-clean-5 = Criminal background check completed with no findings
records-random-imprisonment-outcome-1 = Brief detention in custody
records-random-imprisonment-outcome-1-reason-1 = at a previous posting
records-random-imprisonment-outcome-1-reason-1-suffix-1 = , released early for good behavior
records-random-imprisonment-outcome-1-reason-1-suffix-2 = , sentence served in full
records-random-imprisonment-outcome-1-reason-2 = as part of a corporate investigation
records-random-imprisonment-outcome-1-reason-2-suffix-1 = , charges later dropped
records-random-imprisonment-outcome-1-reason-2-suffix-2 = , case file classified
records-random-imprisonment-outcome-1-reason-3 = over a quarantine violation
records-random-imprisonment-outcome-1-reason-3-suffix-1 = , quarantine lifted early
records-random-imprisonment-outcome-1-reason-3-suffix-2 = , no repeat violations recorded
records-random-imprisonment-outcome-1-reason-4 = on a charge later found to be unfounded
records-random-imprisonment-outcome-1-reason-4-suffix-1 = , the charge was dropped
records-random-imprisonment-outcome-1-reason-4-suffix-2 = , case closed without prosecution
records-random-imprisonment-outcome-2 = Served time at a sector correctional facility
records-random-imprisonment-outcome-2-reason-1 = for a facility security violation
records-random-imprisonment-outcome-2-reason-1-suffix-1 = , conviction expunged after sentence completion
records-random-imprisonment-outcome-2-reason-1-suffix-2 = , rights restored after release
records-random-imprisonment-outcome-2-reason-2 = over a civil claim by the company
records-random-imprisonment-outcome-2-reason-2-suffix-1 = , claim settled out of court
records-random-imprisonment-outcome-2-reason-2-suffix-2 = , ruling overturned on appeal
records-random-imprisonment-outcome-2-reason-3 = for repeated breaches of work discipline
records-random-imprisonment-outcome-2-reason-3-suffix-1 = , positive conduct report from the facility
records-random-imprisonment-outcome-2-reason-3-suffix-2 = , terms of release fully observed
records-random-imprisonment-outcome-2-reason-4 = by corporate tribunal ruling for a criminal offense in office
records-random-imprisonment-outcome-2-reason-4-suffix-1 = , the sentence was appealed
records-random-imprisonment-outcome-2-reason-4-suffix-2 = , conviction not expunged
records-random-imprisonment-outcome-3 = Suspended sentence by NanoTrasen tribunal
records-random-imprisonment-outcome-3-reason-1 = for misconduct in office
records-random-imprisonment-outcome-3-reason-1-suffix-1 = , probation completed without incident
records-random-imprisonment-outcome-3-reason-1-suffix-2 = , sentence reduced for good conduct
records-random-imprisonment-outcome-3-reason-2 = following an internal investigation
records-random-imprisonment-outcome-3-reason-2-suffix-1 = , conviction expunged
records-random-imprisonment-outcome-3-reason-2-suffix-2 = , case reopened in the accused's favor
records-random-imprisonment-outcome-3-reason-3 = for abuse of authority
records-random-imprisonment-outcome-3-reason-3-suffix-1 = , authority temporarily restricted
records-random-imprisonment-outcome-3-reason-3-suffix-2 = , reinstated to their previous position
records-random-imprisonment-outcome-3-reason-4 = following an internal service review
records-random-imprisonment-outcome-3-reason-4-suffix-1 = , the review found no intentional wrongdoing
records-random-imprisonment-outcome-3-reason-4-suffix-2 = , a disciplinary action was issued
records-random-imprisonment-outcome-4 = Held in station isolation during proceedings
records-random-imprisonment-outcome-4-reason-1 = pending clarification of the incident
records-random-imprisonment-outcome-4-reason-1-suffix-1 = , released without charges
records-random-imprisonment-outcome-4-reason-1-suffix-2 = , placed under a travel restriction order
records-random-imprisonment-outcome-4-reason-2 = at the request of station security
records-random-imprisonment-outcome-4-reason-2-suffix-1 = , proceedings closed
records-random-imprisonment-outcome-4-reason-2-suffix-2 = , records transferred to station archive
records-random-imprisonment-outcome-4-reason-3 = for the protection of the injured party pending the outcome
records-random-imprisonment-outcome-4-reason-3-suffix-1 = , the injured party withdrew their statement
records-random-imprisonment-outcome-4-reason-3-suffix-2 = , restrictions were lifted
records-random-imprisonment-outcome-4-reason-4 = by order of the station captain
records-random-imprisonment-outcome-4-reason-4-suffix-1 = , the order was later overturned
records-random-imprisonment-outcome-4-reason-4-suffix-2 = , proceedings referred to a higher authority
records-random-imprisonment-template = {$outcome} {$reason}{$suffix}

records-random-academic-field-1 = Xenobiology
records-random-academic-field-2 = Applied engineering
records-random-academic-field-3 = Quantum mechanics
records-random-academic-field-4 = Space medicine
records-random-academic-field-5 = Colonial economics
records-random-academic-field-6 = Astronavigation
records-random-academic-field-7 = Robotics
records-random-academic-field-8 = Exoplanetology
records-random-academic-field-9 = Nuclear physics
records-random-academic-field-10 = Materials science
records-random-academic-field-11 = Information security

records-random-licenses-1 = Entry-level specialist license
records-random-licenses-2 = No active licenses on file
records-random-licenses-3 = License for research equipment operation
records-random-licenses-4 = NanoTrasen advanced training certificate
records-random-licenses-5 = Light shuttle piloting license
records-random-licenses-6 = License for industrial reactor operation
records-random-licenses-7 = Advanced safety certification
records-random-licenses-8 = License for controlled substance handling
records-random-licenses-9 = Emergency response specialist certificate
records-random-licenses-10 = Permit for remote drone operation
records-random-licenses-11 = License expired, renewal not filed

## Employment history: base statement + optional years-of-experience clause
records-random-employment-history-base-1 = Several contracts in similar roles across other sectors
records-random-employment-history-base-2 = First contract with NanoTrasen
records-random-employment-history-base-3 = Previously worked an adjacent specialty at another station
records-random-employment-history-base-4 = Transferred from a subsidiary division
records-random-employment-history-base-5 = Prior experience in a similar role before transfer
records-random-employment-history-base-6 = Short-term contracts across several sectors
records-random-employment-history-base-7 = Internship followed by transfer to a permanent contract
records-random-employment-history-base-8 = Rotational work experience across several stations
records-random-employment-history-base-9 = Recommended for transfer by a previous supervisor
records-random-employment-history-base-10 = Transferred under the NanoTrasen personnel rotation program
records-random-employment-history-base-11 = Part-time contract work before moving to a full-time position
records-random-employment-history-with-years = {$base}, {$years} {$years ->
    [one] year
   *[other] years
} of total experience

records-random-specialty-1 = Applied cybernetics
records-random-specialty-2 = Space biology
records-random-specialty-3 = Life support engineering
records-random-specialty-4 = Exotic chemistry
records-random-specialty-5 = Administrative management
records-random-specialty-6 = Xenolinguistics
records-random-specialty-7 = Applied astrophysics
records-random-specialty-8 = Applied xenoarchaeology
records-random-specialty-9 = Powerplant engineering
records-random-specialty-10 = Forensic science
records-random-specialty-11 = Long-haul logistics

records-random-institution-1 = Mars Technical Institute
records-random-institution-2 = NanoTrasen Academy
records-random-institution-3 = Lunar University of Applied Sciences
records-random-institution-4 = Institute of Space Medicine
records-random-institution-5 = New Delphi Colonial University
records-random-institution-6 = Tharsis Institute of Xenobiology
records-random-institution-7 = Sector Military Technical Academy
records-random-institution-8 = Earth Institute of Forensic Science
records-random-institution-9 = Ganymede School of Engineering
records-random-institution-10 = Deep Survey Academy
records-random-institution-11 = Ceres Mining College
# Sunrise added end
