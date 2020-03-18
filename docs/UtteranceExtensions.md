# Extending the generic utterance model

The [generic utterance model](GenericUtterances.md) only covers the text, intent and entities for a given NLU scenario. For some NLU providers, we may want to include additional context, such as a confidence score for the intent prediction or a timestamp for when the request was made. This document covers some of the common extensions to the generic utterance model used across the LUIS, Lex, and Dialogflow NLU providers.

## Returning confidence scores for text, intents, and entities

When an NLU provider in NLU.DevOps returns a prediction result, the value will be serialized as-is, meaning any additional properties included in the result will be serialized as well. Currently, LUIS and Dialogflow return text transcription and intent confidence scores in `textScore` and `score` properties, respectively. E.g.:
```json
{
    "text": "play a rock song",
    "intent": "PlayMusic",
    "entities": [
        {
            "matchText": "rock",
            "entityType": "genre",
            "score": 0.80
        }
    ],
    "score": 0.99,
    "textScore": 0.95
}
```
In this case, the intent confidence score was `0.99` and the text transcription confidence score was `0.95`. This is useful context when debugging false predictions, as a low confidence score may indicate that the model could be improved with more training examples. The recognized `genre` entity also includes a confidence score of `0.80`, although it should be noted that only the LUIS provider currently returns confidence score for entity types trained from examples.

## Returning timestamps for each query

When analyzing results for a set of NLU predictions, it is often important context to understand when the test was run. For example, for Dialogflow `date` and `time` entities, the service only returns a date time string, and no indication of what token(s) triggered that entity to be recognized. For example, the result from a query like `"Call a taxi in 15 minutes"` may look like the following:
```json
{
    "text": "call a taxi in 15 minutes",
    "intent": "ScheduleTaxi",
    "entities": [
        {
            "entityType": "time",
            "entityValue": "2020-01-01T00:15:00-04:00"
        }
    ],
    "timestamp": "2020-01-01T00:00:00-04:00"
}
```
Without the context provided by the `timestamp` property, we wouldn't be able to make any assertion about the correctness of the `entityValue` property for time. Currently, you must specify the [`--timestamp`](Test.md#--timestamp) option to ensure a timestamp is assigned to each NLU prediction result.

## Adjusting entity compare results

When running the [`compare`](Analyze.md) command, false positive entity results may or may not be generated.  By default, the `compare` command generates false positive results for any unexpected entities. When using the [`--unit-test`] flag with the `compare` command, false positive entities are ignored by default. The `strictEntities` and `ignoreEntities` extension properties allow you to change this default behavior for a specific utterance.

In the following example, if a `date` entity is recognized, it would not generate a false positive result:
```json
{
    "text": "schedule a meeting for tomorrow",
    "intent": "BookMeeting",
    "entities": [],
    "ignoreEntities": [ "date" ]
}
```

## Utterance Extension Properties

### `score`

The confidence score for the intent in the NLU prediction.

### `textScore`

The confidence score for the text transcription, in case the test was run from speech.

### `timestamp`

The timestamp for when the NLU prediction was made.

### `utteranceId`

Used for aggregation of [`compare`](Analyze.md) command results. 

Each NLU prediction may produce multiple classification results (e.g., true positive intent and true negative entities). When an `utteranceId` is provided on a given set of labeled utterances, it will be included as metadata for each classification result produced when running the [`compare`](Analyze.md) command.

### `strictEntities`

Used when running the [`compare`](Analyze.md) command to ensure false positive results are generated for included entity types, even if they are ignored in the global test settings.

### `ignoreEntities`

Used when running the [`compare`](Analyze.md) command to ensure false positive results are not generated for included entity types, even if they are declared as strict entities in the global test settings.

## Entity Extension Properties

### `score`

The confidence score for the entity in the NLU prediction.
