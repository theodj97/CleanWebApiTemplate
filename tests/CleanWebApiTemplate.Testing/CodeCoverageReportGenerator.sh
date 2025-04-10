#!/bin/bash

RESULTS_DIR="./TestResults"
REPORT_DIR="./coverage-report"
REPORT_FILE="index.html"
RESULT_FILE="coverage.cobertura.xml"

rm -rf $REPORT_DIR
mkdir -p $REPORT_DIR

rm -rf $RESULTS_DIR
mkdir -p $RESULTS_DIR

echo "Executing tests..."
dotnet test --collect:"XPlat Code Coverage"

if ! find "$RESULTS_DIR" -type f -name "$RESULT_FILE" | grep -q .; then
  echo "No coverage report found."
  exit 1
fi

echo "Generating HTML report..."
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:"$REPORT_DIR" -reporttypes:Html


if [ ! -f "$REPORT_DIR/$REPORT_FILE" ]; then
  echo "Report was not generated. Please check the logs for errors."
  exit 1
fi

open "$REPORT_DIR/$REPORT_FILE"

exit 0