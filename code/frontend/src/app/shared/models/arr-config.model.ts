/**
 * Represents an *arr instance with connection details
 */
export interface ArrInstance {
  id?: string;
  name: string;
  url: string;
  apiKey: string;
}

/**
 * DTO for creating new Arr instances without requiring an ID
 */
export interface CreateArrInstanceDto {
  name: string;
  url: string;
  apiKey: string;
}
