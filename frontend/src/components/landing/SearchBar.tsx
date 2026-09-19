import {
  CalendarDays,
  Check,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  MapPin,
  Minus,
  Plus,
  Search,
  Users,
  X,
} from "lucide-react";
import { type FormEvent, useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { navigate } from "../AppLink";

interface DestinationItem {
  name: string;
  parish: string;
  query: string;
}

const DESTINATIONS: DestinationItem[] = [
  { name: "Anywhere in Jamaica", parish: "All parishes & regions", query: "" },
  { name: "Kingston", parish: "Kingston", query: "Kingston" },
  { name: "Montego Bay", parish: "St. James", query: "Montego Bay" },
  { name: "Ocho Rios", parish: "St. Ann", query: "Ocho Rios" },
  { name: "Negril", parish: "Westmoreland", query: "Negril" },
  { name: "Port Antonio", parish: "Portland", query: "Port Antonio" },
  { name: "Treasure Beach", parish: "St. Elizabeth", query: "Treasure Beach" },
  { name: "Falmouth", parish: "Trelawny", query: "Falmouth" },
  { name: "Runaway Bay", parish: "St. Ann", query: "Runaway Bay" },
  { name: "Mandeville", parish: "Manchester", query: "Mandeville" },
];

function formatShortDate(value: string) {
  const date = new Date(`${value}T12:00:00`);
  if (Number.isNaN(date.getTime())) return "";
  return new Intl.DateTimeFormat("en-US", { month: "short", day: "numeric" }).format(date);
}

export default function SearchBar() {
  const [location, setLocation] = useState("");
  const [checkIn, setCheckIn] = useState("");
  const [checkOut, setCheckOut] = useState("");
  const [adults, setAdults] = useState(2);
  const [children, setChildren] = useState(0);
  const [infants, setInfants] = useState(0);

  type ActivePopup = "location" | "dates" | "guests" | null;
  const [activePopup, setActivePopup] = useState<ActivePopup>(null);

  const searchBarRef = useRef<HTMLFormElement>(null);
  const locFieldRef = useRef<HTMLDivElement>(null);
  const dateFieldRef = useRef<HTMLDivElement>(null);
  const guestFieldRef = useRef<HTMLDivElement>(null);

  const [popoverPos, setPopoverPos] = useState<{ top: number; left: number; width?: number }>({
    top: 0,
    left: 0,
  });

  const [viewDate, setViewDate] = useState(() => {
    const d = new Date();
    return new Date(d.getFullYear(), d.getMonth(), 1);
  });

  const [destQuery, setDestQuery] = useState("");

  const totalGuests = adults + children;

  function updatePosition(target: HTMLDivElement | null) {
    if (!target) return;
    const rect = target.getBoundingClientRect();
    const scrollY = window.scrollY;
    const scrollX = window.scrollX;

    const left = rect.left + scrollX;
    const top = rect.bottom + scrollY + 12;

    setPopoverPos({ top, left, width: rect.width });
  }

  function openPopup(popup: ActivePopup) {
    if (activePopup === popup) {
      setActivePopup(null);
      return;
    }
    setActivePopup(popup);

    if (popup === "location") updatePosition(locFieldRef.current);
    if (popup === "dates") updatePosition(dateFieldRef.current);
    if (popup === "guests") updatePosition(guestFieldRef.current);
  }

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      const target = e.target as HTMLElement;
      if (
        searchBarRef.current?.contains(target) ||
        target.closest(".nesty-popover")
      ) {
        return;
      }
      setActivePopup(null);
    }

    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") {
        setActivePopup(null);
      }
    }

    function handleScrollResize() {
      if (!activePopup) return;
      if (activePopup === "location") updatePosition(locFieldRef.current);
      if (activePopup === "dates") updatePosition(dateFieldRef.current);
      if (activePopup === "guests") updatePosition(guestFieldRef.current);
    }

    if (activePopup) {
      document.addEventListener("mousedown", handleClickOutside);
      document.addEventListener("keydown", handleKeyDown);
      window.addEventListener("resize", handleScrollResize);
      window.addEventListener("scroll", handleScrollResize, true);
    }

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      document.removeEventListener("keydown", handleKeyDown);
      window.removeEventListener("resize", handleScrollResize);
      window.removeEventListener("scroll", handleScrollResize, true);
    };
  }, [activePopup]);

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setActivePopup(null);
    const params = new URLSearchParams();
    if (location.trim()) params.set("search", location.trim());
    if (checkIn) params.set("checkIn", checkIn);
    if (checkOut) params.set("checkOut", checkOut);
    params.set("adults", String(adults));
    if (children > 0) params.set("children", String(children));
    if (infants > 0) params.set("infants", String(infants));
    navigate(params.toString() ? `/explore?${params.toString()}` : "/explore");
  }

  const todayStr = new Date().toISOString().slice(0, 10);

  function getMonthDays(year: number, month: number) {
    const firstDay = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    return { firstDay, daysInMonth };
  }

  function handleDateClick(dateStr: string) {
    if (dateStr < todayStr) return;
    if (!checkIn || (checkIn && checkOut)) {
      setCheckIn(dateStr);
      setCheckOut("");
    } else if (checkIn && !checkOut) {
      if (dateStr < checkIn) {
        setCheckIn(dateStr);
      } else if (dateStr === checkIn) {
        setCheckIn(dateStr);
      } else {
        setCheckOut(dateStr);
      }
    }
  }

  const nextMonthDate = new Date(viewDate.getFullYear(), viewDate.getMonth() + 1, 1);

  function prevMonth() {
    setViewDate(new Date(viewDate.getFullYear(), viewDate.getMonth() - 1, 1));
  }

  function nextMonth() {
    setViewDate(new Date(viewDate.getFullYear(), viewDate.getMonth() + 1, 1));
  }

  function renderMonth(year: number, month: number) {
    const { firstDay, daysInMonth } = getMonthDays(year, month);
    const monthName = new Intl.DateTimeFormat("en-US", { month: "long", year: "numeric" }).format(
      new Date(year, month, 1)
    );

    const cells = [];
    for (let i = 0; i < firstDay; i++) {
      cells.push(<div key={`blank-${i}`} className="nesty-cal__cell nesty-cal__cell--empty" />);
    }

    for (let day = 1; day <= daysInMonth; day++) {
      const dayStr = `${year}-${String(month + 1).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
      const isPast = dayStr < todayStr;
      const isToday = dayStr === todayStr;
      const isCheckIn = dayStr === checkIn;
      const isCheckOut = dayStr === checkOut;
      const isInRange = checkIn && checkOut && dayStr > checkIn && dayStr < checkOut;

      let cellClass = "nesty-cal__day";
      if (isPast) cellClass += " nesty-cal__day--disabled";
      if (isToday) cellClass += " nesty-cal__day--today";
      if (isCheckIn) cellClass += " nesty-cal__day--checkin";
      if (isCheckOut) cellClass += " nesty-cal__day--checkout";
      if (isInRange) cellClass += " nesty-cal__day--in-range";

      cells.push(
        <button
          key={dayStr}
          type="button"
          disabled={isPast}
          className={cellClass}
          aria-label={dayStr}
          aria-pressed={isCheckIn || isCheckOut}
          onClick={() => handleDateClick(dayStr)}
        >
          <span>{day}</span>
        </button>
      );
    }

    return (
      <div className="nesty-cal__month">
        <div className="nesty-cal__month-title">{monthName}</div>
        <div className="nesty-cal__weekdays">
          {["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"].map((d) => (
            <span key={d}>{d}</span>
          ))}
        </div>
        <div className="nesty-cal__grid">{cells}</div>
      </div>
    );
  }

  const filteredDestinations = DESTINATIONS.filter(
    (d) =>
      d.name.toLowerCase().includes(destQuery.toLowerCase()) ||
      d.parish.toLowerCase().includes(destQuery.toLowerCase())
  );

  return (
    <>
      <form
        ref={searchBarRef}
        className="reference-search"
        onSubmit={submitSearch}
        aria-label="Find a stay"
      >
        {/* Where to? */}
        <div
          ref={locFieldRef}
          className={`reference-search__field reference-search__field--location ${
            activePopup === "location" ? "reference-search__field--active" : ""
          }`}
          onClick={() => openPopup("location")}
          role="button"
          tabIndex={0}
          onKeyDown={(e) => {
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              openPopup("location");
            }
          }}
          aria-expanded={activePopup === "location"}
          aria-label="Where to?"
        >
          <MapPin aria-hidden="true" size={21} strokeWidth={1.8} />
          <span>
            <strong>Where to?</strong>
            <span aria-hidden="true" className="reference-search__value">
              {location || "e.g. Kingston, Ocho Rios"}
            </span>
          </span>
        </div>

        {/* Check in — Check out */}
        <div
          ref={dateFieldRef}
          className={`reference-search__field reference-search__field--dates ${
            activePopup === "dates" ? "reference-search__field--active" : ""
          }`}
          onClick={() => openPopup("dates")}
          role="button"
          tabIndex={0}
          onKeyDown={(e) => {
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              openPopup("dates");
            }
          }}
          aria-expanded={activePopup === "dates"}
          aria-label="Check in — Check out"
        >
          <CalendarDays aria-hidden="true" size={21} strokeWidth={1.8} />
          <span>
            <strong>Check in — Check out</strong>
            <span aria-hidden="true" className="reference-search__value">
              {checkIn && checkOut
                ? `${formatShortDate(checkIn)} – ${formatShortDate(checkOut)}`
                : checkIn
                ? `${formatShortDate(checkIn)} – Add check out`
                : "Add dates"}
            </span>
          </span>
        </div>

        {/* Guests */}
        <div
          ref={guestFieldRef}
          className={`reference-search__field reference-search__field--guests ${
            activePopup === "guests" ? "reference-search__field--active" : ""
          }`}
          onClick={() => openPopup("guests")}
          role="button"
          tabIndex={0}
          onKeyDown={(e) => {
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              openPopup("guests");
            }
          }}
          aria-expanded={activePopup === "guests"}
          aria-label="Guests"
        >
          <Users aria-hidden="true" size={21} strokeWidth={1.8} />
          <span>
            <strong>Guests</strong>
            <span aria-hidden="true" className="reference-search__value">
              {totalGuests === 1 ? "1 guest" : `${totalGuests} guests`}
              {infants > 0 ? `, ${infants} infant${infants > 1 ? "s" : ""}` : ""}
            </span>
          </span>
          <ChevronDown aria-hidden="true" className="reference-search__chevron" size={16} />
        </div>

        {/* Submit */}
        <button className="reference-search__submit" type="submit" aria-label="Search stays">
          <Search aria-hidden="true" size={22} strokeWidth={2} />
        </button>
      </form>

      {/* Popovers Portal */}
      {typeof document !== "undefined" &&
        activePopup === "location" &&
        createPortal(
          <div
            className="nesty-popover nesty-popover--location"
            style={{
              top: `${popoverPos.top}px`,
              left: `${Math.max(16, Math.min(window.innerWidth - 420, popoverPos.left))}px`,
            }}
          >
            <div className="nesty-popover__header">
              <h3 className="nesty-popover__title">Where yuh heading?</h3>
              <p className="nesty-popover__subtitle">Pick a spot across Jamaica.</p>
            </div>

            <div className="nesty-popover__search-box">
              <Search size={16} strokeWidth={2} className="nesty-popover__search-icon" />
              <input
                type="text"
                placeholder="Search destination"
                value={destQuery}
                onChange={(e) => setDestQuery(e.target.value)}
                autoFocus
                className="nesty-popover__input"
              />
              {destQuery && (
                <button
                  type="button"
                  onClick={() => setDestQuery("")}
                  className="nesty-popover__clear-btn"
                  aria-label="Clear destination search"
                >
                  <X size={14} />
                </button>
              )}
            </div>

            <div className="nesty-popover__list">
              {filteredDestinations.map((dest) => {
                const isSelected =
                  (!dest.query && !location) || (dest.query && location === dest.query);
                return (
                  <button
                    type="button"
                    key={dest.name}
                    className={`nesty-dest-row ${isSelected ? "nesty-dest-row--selected" : ""}`}
                    onClick={() => {
                      setLocation(dest.query);
                      setActivePopup("dates");
                      updatePosition(dateFieldRef.current);
                    }}
                  >
                    <div className="nesty-dest-row__icon">
                      <MapPin size={18} strokeWidth={1.8} />
                    </div>
                    <div className="nesty-dest-row__info">
                      <span className="nesty-dest-row__name">{dest.name}</span>
                      <span className="nesty-dest-row__parish">{dest.parish}</span>
                    </div>
                    {isSelected && (
                      <div className="nesty-dest-row__check">
                        <Check size={16} strokeWidth={2.5} />
                      </div>
                    )}
                  </button>
                );
              })}
              {filteredDestinations.length === 0 && (
                <div className="nesty-popover__empty">No spots found matching "{destQuery}"</div>
              )}
            </div>
          </div>,
          document.body
        )}

      {typeof document !== "undefined" &&
        activePopup === "dates" &&
        createPortal(
          <div
            className="nesty-popover nesty-popover--dates"
            style={{
              top: `${popoverPos.top}px`,
              left: `${Math.max(16, Math.min(window.innerWidth - 660, popoverPos.left - 120))}px`,
            }}
          >
            <div className="nesty-popover__header nesty-popover__header--flex">
              <div>
                <h3 className="nesty-popover__title">When yuh staying?</h3>
                <p className="nesty-popover__subtitle">Choose your check-in and check-out dates.</p>
              </div>
              <div className="nesty-cal__nav">
                <button
                  type="button"
                  className="nesty-cal__nav-btn"
                  onClick={prevMonth}
                  aria-label="Previous month"
                >
                  <ChevronLeft size={18} />
                </button>
                <button
                  type="button"
                  className="nesty-cal__nav-btn"
                  onClick={nextMonth}
                  aria-label="Next month"
                >
                  <ChevronRight size={18} />
                </button>
              </div>
            </div>

            <div className="nesty-cal__months-wrap">
              {renderMonth(viewDate.getFullYear(), viewDate.getMonth())}
              <div className="nesty-cal__month-desktop-only">
                {renderMonth(nextMonthDate.getFullYear(), nextMonthDate.getMonth())}
              </div>
            </div>

            <div className="nesty-popover__footer">
              <button
                type="button"
                className="nesty-popover__action-text"
                onClick={() => {
                  setCheckIn("");
                  setCheckOut("");
                }}
              >
                Clear dates
              </button>
              <button
                type="button"
                className="nesty-popover__action-btn"
                onClick={() => {
                  setActivePopup("guests");
                  updatePosition(guestFieldRef.current);
                }}
              >
                Done
              </button>
            </div>
          </div>,
          document.body
        )}

      {typeof document !== "undefined" &&
        activePopup === "guests" &&
        createPortal(
          <div
            className="nesty-popover nesty-popover--guests"
            style={{
              top: `${popoverPos.top}px`,
              left: `${Math.max(16, Math.min(window.innerWidth - 380, popoverPos.left - 160))}px`,
            }}
          >
            <div className="nesty-popover__header">
              <h3 className="nesty-popover__title">Who coming?</h3>
              <p className="nesty-popover__subtitle">Up to 16 guests allowed.</p>
            </div>

            <div className="nesty-guests__list">
              {/* Adults */}
              <div className="nesty-guests__row">
                <div>
                  <div className="nesty-guests__type">Adults</div>
                  <div className="nesty-guests__age">Ages 13+</div>
                </div>
                <div className="nesty-counter">
                  <button
                    type="button"
                    className="nesty-counter__btn"
                    disabled={adults <= 1}
                    onClick={() => setAdults((prev) => Math.max(1, prev - 1))}
                    aria-label="Decrease adults"
                  >
                    <Minus size={14} strokeWidth={2.2} />
                  </button>
                  <span className="nesty-counter__val">{adults}</span>
                  <button
                    type="button"
                    className="nesty-counter__btn"
                    disabled={totalGuests >= 16}
                    onClick={() => setAdults((prev) => prev + 1)}
                    aria-label="Increase adults"
                  >
                    <Plus size={14} strokeWidth={2.2} />
                  </button>
                </div>
              </div>

              {/* Children */}
              <div className="nesty-guests__row">
                <div>
                  <div className="nesty-guests__type">Children</div>
                  <div className="nesty-guests__age">Ages 2–12</div>
                </div>
                <div className="nesty-counter">
                  <button
                    type="button"
                    className="nesty-counter__btn"
                    disabled={children <= 0}
                    onClick={() => setChildren((prev) => Math.max(0, prev - 1))}
                    aria-label="Decrease children"
                  >
                    <Minus size={14} strokeWidth={2.2} />
                  </button>
                  <span className="nesty-counter__val">{children}</span>
                  <button
                    type="button"
                    className="nesty-counter__btn"
                    disabled={totalGuests >= 16}
                    onClick={() => setChildren((prev) => prev + 1)}
                    aria-label="Increase children"
                  >
                    <Plus size={14} strokeWidth={2.2} />
                  </button>
                </div>
              </div>

              {/* Infants */}
              <div className="nesty-guests__row">
                <div>
                  <div className="nesty-guests__type">Infants</div>
                  <div className="nesty-guests__age">Under 2</div>
                </div>
                <div className="nesty-counter">
                  <button
                    type="button"
                    className="nesty-counter__btn"
                    disabled={infants <= 0}
                    onClick={() => setInfants((prev) => Math.max(0, prev - 1))}
                    aria-label="Decrease infants"
                  >
                    <Minus size={14} strokeWidth={2.2} />
                  </button>
                  <span className="nesty-counter__val">{infants}</span>
                  <button
                    type="button"
                    className="nesty-counter__btn"
                    disabled={infants >= 5}
                    onClick={() => setInfants((prev) => prev + 1)}
                    aria-label="Increase infants"
                  >
                    <Plus size={14} strokeWidth={2.2} />
                  </button>
                </div>
              </div>
            </div>

            <div className="nesty-popover__footer">
              <button
                type="button"
                className="nesty-popover__action-text"
                onClick={() => {
                  setAdults(2);
                  setChildren(0);
                  setInfants(0);
                }}
              >
                Clear
              </button>
              <button
                type="button"
                className="nesty-popover__action-btn"
                onClick={() => setActivePopup(null)}
              >
                Done
              </button>
            </div>
          </div>,
          document.body
        )}
    </>
  );
}
