import { useCallback, useEffect, useMemo, useState } from "react";
import { api, type PropertyListing } from "../lib/api";

export function useProperties() {
  const [properties, setProperties] = useState<PropertyListing[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      setProperties(await api.getProperties());
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Properties could not be loaded.");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  return useMemo(
    () => ({ properties, isLoading, error, reload, setProperties }),
    [error, isLoading, properties, reload],
  );
}

export function useProperty(propertyId?: string) {
  const [property, setProperty] = useState<PropertyListing | null>(null);
  const [isLoading, setIsLoading] = useState(Boolean(propertyId));
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!propertyId) {
      setProperty(null);
      setIsLoading(false);
      setError("Property id is missing.");
      return;
    }

    let cancelled = false;
    setIsLoading(true);
    setError(null);

    api
      .getProperty(propertyId)
      .then((result) => {
        if (!cancelled) setProperty(result);
      })
      .catch((caught) => {
        if (!cancelled) {
          setError(caught instanceof Error ? caught.message : "Property could not be loaded.");
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [propertyId]);

  return { property, isLoading, error };
}
