import { useCallback, useEffect, useMemo, useState } from "react";
import { api, type Booking } from "../lib/api";

export function useBookings(guestUserId?: string) {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      setBookings(await api.getBookings(guestUserId));
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Bookings could not be loaded.");
    } finally {
      setIsLoading(false);
    }
  }, [guestUserId]);

  useEffect(() => {
    void reload();
  }, [reload]);

  return useMemo(
    () => ({ bookings, isLoading, error, reload, setBookings }),
    [bookings, error, isLoading, reload],
  );
}
