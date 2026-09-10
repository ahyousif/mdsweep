export type TripImportProblem = {
  rowNumber: number | null;
  tripNumber: string | null;
  field: string | null;
  message: string;
};

export type TripImportSummary = {
  readyCount: number;
  needsAttentionCount: number;
  problems: TripImportProblem[];
};
