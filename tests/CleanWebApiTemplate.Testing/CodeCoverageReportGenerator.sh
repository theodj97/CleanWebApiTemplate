#!/bin/bash

RESULTS_DIR="./TestResults"
REPORT_DIR="./coverage-report"
REPORT_FILE="index.html"
RESULT_FILE="coverage.cobertura.xml"

rm -rf $REPORT_DIR
mkdir -p $REPORT_DIR

rm -rf $RESULTS_DIR
mkdir -p $RESULTS_DIR

echo "Executing tests with code coverage (Microsoft.Testing.Platform)..."
dotnet test --results-directory "$RESULTS_DIR" --coverage --coverage-output-format cobertura --coverage-output "$RESULT_FILE"

if ! find "$RESULTS_DIR" -type f -name "$RESULT_FILE" | grep -q .; then
  echo "No coverage report found."
  exit 1
fi

echo "Generating HTML report..."
# -filefilters excludes source-generated files (obj/**/*.g.cs) so the report
# only reflects hand-written code under src/
reportgenerator -reports:"$RESULTS_DIR/$RESULT_FILE" -targetdir:"$REPORT_DIR" -reporttypes:Html -filefilters:"-*.g.cs;-*.generated.cs"


if [ ! -f "$REPORT_DIR/$REPORT_FILE" ]; then
  echo "Report was not generated. Please check the logs for errors."
  exit 1
fi

open "$REPORT_DIR/$REPORT_FILE"

exit 0
