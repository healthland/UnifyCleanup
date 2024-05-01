## Identify the IM duplicate identifiers to be removed

1. Using the JSON file generated from the spreadsheet All Organizations that have a partitiontable and have records
2. Pass the parm for the environment DEV, QA, or PD
3. The connection string for the FHIR servers will accessed using DBConnections
4. The FHIR table and OrganizationId will be used from the JSON file
5. The DuplicateIdentifiers sql will be ran against the FHIR partition table to get the identifiervalue that is duplicated
6. The OrganizationId and IdentifierValue will be added to IMDuplicates.json to use for the duplicate removals

The following lists the duplicate identifers

```SQL
SELECT   "IdentifierSystem", "IdentifierValue", "PartitionKey", COUNT(*) 
FROM public."TPatientInfo_387"
Where "IdentifierValue" like 'id-%' and ("IdentifierUse" = '' or "IdentifierUse" = 'Old')
group by  "PartitionKey", "IdentifierSystem", "IdentifierValue"
HAVING COUNT(*) > 1
ORDER BY  "PartitionKey" ASC
```

Before running the tool, JSON file DuplicateInput.json will need to be created and added to the files folder for the partition tables and organization to use.
```
Ex. of DuplicateInput.json
[
 {
   "OrganizationId": 387,
   "DBConnectionId": 1,
   "Organization": "de83f66a-d66f-4a17-963d-a5c1510c1ddb"
 },
 {
   "OrganizationId": 432,
   "DBConnectionId": 1,
   "Organization": "d7e239b1-e975-47bd-a313-36368276bcdb"
 },
 {
   "OrganizationId": 447,
   "DBConnectionId": 1,
   "Organization": "805b452b-163e-47a9-9a70-194d556e5c64"
 },
 {
   "OrganizationId": 303,
   "DBConnectionId": 2,
   "Organization": "edbf23fa-3ce0-4075-88f0-33d2f00f2bac"
 }
]


```

### Usage

```powershell
.\RemoveDuplicateIdentifier.exe -o "PD""
```

#### Help

```powershell
 .\RemoveDuplicateIdentifer.exe --help
```

### Note

If this tool need to be rerun might need to clear the IMDuplicates.JSON under //Files

The IMDuplicates.JSON example output:
[
{
   "organizationId": "805b452b-163e-47a9-9a70-194d556e5c64",
   "IdentifierValue": "id-58.58.16655"
},
{
   "organizationId": "805b452b-163e-47a9-9a70-194d556e5c64",
   "IdentifierValue": "id-58.58.16774"
}
]

```

