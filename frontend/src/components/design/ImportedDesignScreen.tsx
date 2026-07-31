import { useEffect, useMemo, useRef, useState } from "react";
import { API_BASE_URL } from "../../lib/api";

type DesignScreensPayload = {
  screens: Record<string, string>;
};

const DATA_URL = "/data/nesty-design-screens.json";
const LOGO_URL = "/assets/nesty/Nesty-Stay.png";

const prototypeRoutes: Record<string, string> = {
  "INDEX.dc.html": "/",
  "PUB-01.dc.html": "/",
  "PUB-02.dc.html": "/explore",
  "PUB-04.dc.html": "/properties/montego-bay-standard-apartment",
  "PUB-MAP.html": "/explore/map",
  "PUB-SOON.dc.html": "/coming-soon",
  "AUTH-01.dc.html": "/login",
  "AUTH-01.dc.html#register": "/register#register",
  "AUTH-POST.dc.html": "/auth/post-login-toast",
  "AUTH-LOGOUT.dc.html": "/logout",
  "BOOK-01.dc.html": "/booking/demo/review",
  "BOOK-02.dc.html": "/booking/demo/quote",
  "BOOK-03.dc.html": "/booking/demo/identity",
  "BOOK-05.dc.html": "/booking/demo/checkout",
  "BOOK-07.dc.html": "/booking/demo/pending",
  "BOOK-CONF.dc.html": "/booking/demo/success",
  "TRAV-01.dc.html": "/guest-dashboard",
  "TRAV-COL.dc.html": "/traveler/favorites",
  "TRAV-INV.dc.html": "/traveler/invoices",
  "TRAV-PEND.dc.html": "/traveler/reviews/pending",
  "TRAV-NOTIF.dc.html": "/traveler/notifications",
  "TRAV-SUGG.dc.html": "/traveler/suggestions",
  "TRAV-12.dc.html": "/profile",
  "MSG-01.dc.html": "/messages",
  "MSG-DOC.dc.html": "/messages/document",
  "HOST-01.dc.html": "/host-dashboard",
  "HOST-05.dc.html": "/host/properties",
  "HOST-EDIT.dc.html": "/host/properties/edit",
  "HOST-RPT.dc.html": "/host/reports",
  "HOST-WELL.dc.html": "/host/wellness",
  "HOST-BADGE.dc.html": "/host/badges",
  "PM-GATE.dc.html": "/pm/gates",
  "PM-UTIL.dc.html": "/pm/utilities",
  "PM-VERIFY.dc.html": "/pm/verification",
  "PM-RPT.dc.html": "/pm/reports",
  "PM-INS.dc.html": "/pm/insurance",
  "OFC-01.dc.html": "/officer/wellness",
  "OFC-02.dc.html": "/officer/wellness",
  "OFC-DIR.dc.html": "/host/wellness/directory",
  "OFC-BOOK.dc.html": "/host/wellness/book",
  "DIR-02.dc.html": "/directory/trades",
  "DIR-BIZ.dc.html": "/directory/businesses",
  "DIR-PROV.dc.html": "/directory/provider",
  "ADM-01.dc.html": "/admin/ops/disputes",
  "ADM-KPI.dc.html": "/admin/kpis",
  "ADM-RPT.dc.html": "/admin/reports",
  "ADM-RESET.dc.html": "/admin/officer-id-reset",
  "ERR-401.dc.html": "/401",
  "ERR-403.dc.html": "/403",
  "ERR-404.dc.html": "/404",
  "ERR-500.dc.html": "/500",
  "ERR-NOFAV.dc.html": "/empty/favorites",
  "ERR-NORES.dc.html": "/empty/reservations",
  "ERR-LOAD.dc.html": "/screens/ERR-LOAD",
  "DS-V2.dc.html": "/screens/DS-V2",
};

const screenAliases: Record<string, string> = {
  "PUB-MAP": "PUB-MAP",
  "DS-01": "DS-V2",
};

let cachedScreens: Promise<Record<string, string>> | null = null;

function loadScreens() {
  cachedScreens ??= fetch(DATA_URL)
    .then((response) => {
      if (!response.ok) {
        throw new Error(`Could not load ${DATA_URL}`);
      }
      return response.json() as Promise<DesignScreensPayload>;
    })
    .then((payload) => payload.screens);

  return cachedScreens;
}

function mapPrototypeHref(href: string) {
  const cleanHref = href.trim().replace(/^\.\//, "");

  if (!cleanHref || cleanHref === "#") return cleanHref;
  if (cleanHref === "#register") return "/register#register";
  if (cleanHref === "#recover") return "/auth/forgot-password#recover";
  if (cleanHref.startsWith("#")) return cleanHref;
  if (/^(https?:|mailto:|tel:)/i.test(cleanHref)) return cleanHref;

  const direct = prototypeRoutes[cleanHref];
  if (direct) return direct;

  const [withoutHash, hash] = cleanHref.split("#");
  const base = prototypeRoutes[withoutHash];
  if (!base) return cleanHref;

  if (hash === "register") return "/register#register";
  if (hash === "recover") return "/auth/forgot-password#recover";
  return base;
}

function rewriteLinks(html: string) {
  return html.replace(/\shref=(["'])(.*?)\1/gi, (match, quote: string, href: string) => {
    const mapped = mapPrototypeHref(href);
    if (!mapped || mapped === href) return match;

    const target = mapped.startsWith("/") ? " data-nesty-route=\"true\"" : "";
    return ` href=${quote}${mapped}${quote}${target}`;
  });
}

function prepareScreenHtml(screenId: string, html: string, apiBaseUrl: string, currentPath: string) {
  const [rawScreenId, rawAnchorId] = screenId.split("#");
  const resolvedScreenId = screenAliases[rawScreenId] ?? rawScreenId;
  const anchorId = rawAnchorId ?? "";
  const bridge = `
<script>
(function () {
  var initialAnchor = ${JSON.stringify(anchorId)};
  var resolvedScreenId = ${JSON.stringify(resolvedScreenId)};
  var apiBaseUrl = ${JSON.stringify(apiBaseUrl)};
  var currentPath = ${JSON.stringify(currentPath)};
  var sessionStorageKey = "nestyStay.session";
  var bookingDraftStorageKey = "nestyStay.bookingDraft";

  function notifyHeight() {
    var body = document.body;
    var root = document.documentElement;
    var height = Math.max(
      body ? body.scrollHeight : 0,
      root ? root.scrollHeight : 0,
      window.innerHeight || 0
    );
    window.parent.postMessage({ type: "nesty-screen-height", screenId: resolvedScreenId, height: height }, "*");
  }

  function notifyAnchor(target) {
    var anchorTarget = target;
    if (!anchorTarget && initialAnchor) {
      anchorTarget = document.getElementById(initialAnchor) || document.querySelector("[name='" + initialAnchor + "']");
    }
    if (!anchorTarget) return;
    window.parent.postMessage({
      type: "nesty-screen-anchor",
      screenId: resolvedScreenId,
      top: anchorTarget.getBoundingClientRect().top + window.scrollY
    }, "*");
  }

  function sendRoute(href) {
    window.parent.postMessage({ type: "nesty-route", href: href }, "*");
  }

  function compactText(value) {
    return (value || "").replace(/\\s+/g, " ").trim();
  }

  function findSectionByHeading(text) {
    var wanted = text.toLowerCase();
    return Array.prototype.find.call(document.querySelectorAll("section, main, div"), function (node) {
      var heading = node.querySelector && node.querySelector("h1,h2,h3");
      return heading && compactText(heading.textContent).toLowerCase() === wanted;
    });
  }

  function findAction(root, text) {
    var wanted = text.toLowerCase();
    return Array.prototype.find.call((root || document).querySelectorAll("button,a"), function (node) {
      return compactText(node.textContent).toLowerCase().indexOf(wanted) !== -1;
    });
  }

  function fieldByHint(root, hints, type) {
    var inputs = Array.prototype.slice.call((root || document).querySelectorAll("input"));
    return inputs.find(function (input) {
      var haystack = [
        input.name,
        input.id,
        input.type,
        input.placeholder,
        input.getAttribute("aria-label")
      ].join(" ").toLowerCase();
      return (!type || input.type === type) && hints.some(function (hint) { return haystack.indexOf(hint) !== -1; });
    }) || inputs.find(function (input) { return !type || input.type === type; });
  }

  function setStatus(root, message, kind) {
    if (!root) return;
    var status = root.querySelector("[data-nesty-status]");
    if (!status) {
      status = document.createElement("div");
      status.setAttribute("data-nesty-status", "true");
      status.style.borderRadius = "14px";
      status.style.padding = "12px 14px";
      status.style.margin = "12px 0";
      status.style.fontSize = "13px";
      status.style.lineHeight = "1.35";
      status.style.fontWeight = "700";
      status.style.border = "1px solid transparent";
      var action = root.querySelector("button:last-of-type");
      if (action && action.parentNode === root) {
        root.insertBefore(status, action);
      } else {
        root.appendChild(status);
      }
    }
    status.textContent = message;
    if (kind === "error") {
      status.style.color = "#7f1d1d";
      status.style.background = "#fef2f2";
      status.style.borderColor = "#fecaca";
    } else {
      status.style.color = "#14532d";
      status.style.background = "#f0fdf4";
      status.style.borderColor = "#bbf7d0";
    }
    notifyHeight();
  }

  function setBusy(button, busy, label) {
    if (!button) return;
    if (!button.dataset.nestyOriginalLabel) {
      button.dataset.nestyOriginalLabel = compactText(button.textContent);
    }
    button.textContent = busy ? label : button.dataset.nestyOriginalLabel;
    button.setAttribute("aria-busy", busy ? "true" : "false");
    if ("disabled" in button) button.disabled = busy;
    button.style.opacity = busy ? "0.72" : "";
  }

  async function api(path, options) {
    var headers = Object.assign({ "Content-Type": "application/json" }, (options && options.headers) || {});
    if (options && options.token) {
      var session = getSession();
      if (session && session.accessToken) {
        headers.Authorization = "Bearer " + session.accessToken;
      }
    }

    var response = await fetch(apiBaseUrl + path, {
      method: (options && options.method) || "GET",
      headers: headers,
      body: options && options.body ? JSON.stringify(options.body) : undefined
    });
    var text = await response.text();
    var data = text ? JSON.parse(text) : null;
    if (!response.ok) {
      throw new Error((data && (data.message || data.error || data.title)) || text || "The server could not complete that request.");
    }
    return data;
  }

  function getSession() {
    try {
      return JSON.parse(localStorage.getItem(sessionStorageKey) || "null");
    } catch (error) {
      return null;
    }
  }

  function formatMoney(amount, currency) {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: currency || "USD",
      maximumFractionDigits: Number(amount) % 1 === 0 ? 0 : 2
    }).format(Number(amount) || 0);
  }

  function formatPropertyLocation(property) {
    var parts = String(property.location || "").split(",").map(function (part) { return part.trim(); }).filter(Boolean);
    if (parts.length >= 2) return parts[0] + " · " + parts[1] + " parish";
    return property.location || property.country || "Jamaica";
  }

  function formatMonthDay(value) {
    return new Date(value).toLocaleDateString("en-US", { month: "short", day: "numeric", timeZone: "UTC" });
  }

  function formatStayRange(checkIn, checkOut) {
    var start = formatMonthDay(checkIn);
    var end = formatMonthDay(checkOut);
    var startMonth = start.split(" ")[0];
    var endParts = end.split(" ");
    return startMonth === endParts[0] ? start + " – " + endParts[1] : start + " – " + end;
  }

  function formatBadge(level, hostLabel) {
    var value = String(level || "Free").toUpperCase();
    if (value === "WELLNESS") return hostLabel ? "◆ WELLNESS HOST" : "◆ WELLNESS";
    if (value === "TRUSTED") return hostLabel ? "★ TRUSTED HOST" : "★ TRUSTED";
    if (value === "VERIFIED") return hostLabel ? "✓ VERIFIED HOST" : "✓ VERIFIED";
    return hostLabel ? "FREE HOST" : "FREE";
  }

  function slugify(value) {
    return String(value || "")
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/^-+|-+$/g, "");
  }

  function leafNodes(root) {
    return Array.prototype.slice.call((root || document).querySelectorAll("h1,h2,h3,h4,p,span,div,strong,button,a,label"));
  }

  function replaceExactText(oldText, newText, root) {
    if (!newText) return;
    leafNodes(root).forEach(function (node) {
      if (node.children.length === 0 && compactText(node.textContent) === oldText) {
        node.textContent = newText;
      }
    });
  }

  function replaceContainingText(fragment, newText, root) {
    if (!newText) return;
    leafNodes(root).forEach(function (node) {
      if (node.children.length === 0 && compactText(node.textContent).indexOf(fragment) !== -1) {
        node.textContent = newText;
      }
    });
  }

  function replaceMetricValue(label, value) {
    var labelNode = leafNodes().find(function (node) {
      return node.children.length === 0 && compactText(node.textContent) === label;
    });
    if (!labelNode || !labelNode.parentElement) return;

    var valueNode = Array.prototype.find.call(labelNode.parentElement.children, function (node) {
      return node !== labelNode && node.children.length === 0 && compactText(node.textContent);
    });
    if (valueNode) valueNode.textContent = String(value);
  }

  function addLiveBadge(message) {
    if (document.querySelector("[data-nesty-live-badge]")) return;
    var badge = document.createElement("div");
    badge.setAttribute("data-nesty-live-badge", "true");
    badge.textContent = message;
    badge.style.position = "fixed";
    badge.style.right = "16px";
    badge.style.bottom = "16px";
    badge.style.zIndex = "9999";
    badge.style.borderRadius = "999px";
    badge.style.padding = "8px 12px";
    badge.style.background = "#E3F2E9";
    badge.style.color = "#135A38";
    badge.style.border = "1px solid #BBF7D0";
    badge.style.fontSize = "12px";
    badge.style.fontWeight = "800";
    badge.style.boxShadow = "0 10px 24px rgba(6,43,43,0.12)";
    document.body.appendChild(badge);
    notifyHeight();
  }

  function propertyKeyFromPath() {
    var propertyMatch = currentPath.match(/\\/properties\\/([^\\/?#]+)/);
    if (propertyMatch) return decodeURIComponent(propertyMatch[1]);
    var bookingMatch = currentPath.match(/\\/booking\\/([^\\/?#]+)/);
    if (bookingMatch && bookingMatch[1] !== "demo") return decodeURIComponent(bookingMatch[1]);
    return "";
  }

  function bookingIdFromPath() {
    var bookingMatch = currentPath.match(/\\/booking\\/([0-9a-f-]{36})\\/(pending|success|confirmed|confirmation)/i);
    if (bookingMatch) return decodeURIComponent(bookingMatch[1]);
    return localStorage.getItem("nestyStay.lastBookingId") || "";
  }

  function selectedProperty(properties) {
    var key = propertyKeyFromPath();
    if (!key) return properties[0];
    return properties.find(function (property) {
      return property.id === key || slugify(property.title) === key;
    }) || properties[0];
  }

  function bookingDraftFor(property) {
    try {
      var draft = JSON.parse(localStorage.getItem(bookingDraftStorageKey) || "null");
      if (draft && draft.propertyId === property.id) return draft;
    } catch (error) {
      // Ignore malformed local draft data.
    }

    return {
      propertyId: property.id,
      checkIn: "2026-12-12",
      checkOut: "2026-12-18",
      adults: 2,
      children: 0,
      protectionPlan: property.insuraGuestEnabled ? "InsuraGuest" : "None"
    };
  }

  function saveBookingDraft(property) {
    var draft = bookingDraftFor(property);
    localStorage.setItem(bookingDraftStorageKey, JSON.stringify(draft));
    return draft;
  }

  async function quoteFor(property) {
    var draft = bookingDraftFor(property);
    return api("/bookings/quote", {
      method: "POST",
      body: {
        propertyId: property.id,
        checkIn: draft.checkIn,
        checkOut: draft.checkOut,
        adults: draft.adults || 2,
        children: draft.children || 0,
        protectionPlan: draft.protectionPlan || "None"
      }
    });
  }

  function hydrateQuoteText(quote) {
    var stayLine = formatMoney(quote.nightlyRate, quote.currency) + " × " + quote.nights + " nights";
    var feeLine = "Service fee (" + Math.round((quote.guestPlatformFee / Math.max(quote.staySubtotal, 1)) * 100) + "%)";
    replaceContainingText("Stay — $450", "Stay — " + stayLine);
    replaceExactText("$450 × 6 nights", stayLine);
    replaceExactText("$320 × 4 nights", stayLine);
    replaceExactText("$2,700", formatMoney(quote.staySubtotal, quote.currency));
    replaceExactText("$1,280.00", formatMoney(quote.staySubtotal, quote.currency));
    replaceExactText("Service fee (10%)", feeLine);
    replaceExactText("Service fee (10% tier)", feeLine + " tier");
    replaceContainingText("Service fee (10% tier)", feeLine + " tier");
    replaceExactText("$270", formatMoney(quote.guestPlatformFee, quote.currency));
    replaceExactText("$128.00", formatMoney(quote.guestPlatformFee, quote.currency));
    replaceExactText("$2,970", formatMoney(quote.totalAmount, quote.currency));
    replaceExactText("$1,408.00", formatMoney(quote.totalAmount, quote.currency));
    replaceExactText("Total (USD)", "Total (" + quote.currency + ")");
    replaceContainingText("Available — 6 nights", "✓ Available — " + quote.nights + " nights, Dec 12 – 18");
    replaceContainingText("Traveler eKYC verification", quote.requiresGuestVerification ? "Traveler eKYC verification" : "Traveler eKYC verification not required");
  }

  function wirePropertyActions(properties) {
    var cards = Array.prototype.slice.call(document.querySelectorAll("article"));

    cards.forEach(function (card, index) {
      var property = properties[index % properties.length];
      Array.prototype.forEach.call(card.querySelectorAll("a,button"), function (action) {
        var label = compactText(action.textContent);
        if (label === "Details") {
          action.setAttribute("href", "/properties/" + property.id);
          action.addEventListener("click", function (event) {
            event.preventDefault();
            sendRoute("/properties/" + property.id);
          });
        }

        if (label === "Book") {
          action.setAttribute("href", "/booking/" + property.id + "/review");
          action.addEventListener("click", function (event) {
            event.preventDefault();
            saveBookingDraft(property);
            sendRoute("/booking/" + property.id + "/review");
          });
        }
      });
    });

    var detailsButtons = leafNodes().filter(function (node) { return compactText(node.textContent) === "Details"; });
    var bookButtons = leafNodes().filter(function (node) { return compactText(node.textContent) === "Book"; });

    detailsButtons.forEach(function (button, index) {
      var property = properties[index % properties.length];
      button.addEventListener("click", function (event) {
        event.preventDefault();
        sendRoute("/properties/" + property.id);
      });
    });

    bookButtons.forEach(function (button, index) {
      var property = properties[index % properties.length];
      button.addEventListener("click", function (event) {
        event.preventDefault();
        saveBookingDraft(property);
        sendRoute("/booking/" + property.id + "/review");
      });
    });

    var bookThisStay = findAction(document, "Book this stay");
    if (bookThisStay) {
      bookThisStay.addEventListener("click", function (event) {
        event.preventDefault();
        var property = selectedProperty(properties);
        saveBookingDraft(property);
        sendRoute("/booking/" + property.id + "/review");
      });
    }
  }

  async function hydratePublicScreens() {
    if (["PUB-01", "PUB-02", "PUB-04"].indexOf(resolvedScreenId) === -1) return;

    try {
      var properties = await api("/properties");
      if (!Array.isArray(properties) || properties.length === 0) return;

      addLiveBadge("Live API · " + properties.length + " stays");
      wirePropertyActions(properties);

      if (resolvedScreenId === "PUB-02") {
        var staticTitles = ["Seaview Villa", "Cliffside Retreat", "Garden Cottage", "Uptown Loft", "Bay Suite", "Reef House"];
        var staticLocations = [
          "Ocho Rios · St. Ann parish",
          "Negril · Westmoreland parish",
          "Port Antonio · Portland parish",
          "Kingston · St. Andrew parish",
          "Montego Bay · St. James parish",
          "Falmouth · Trelawny parish"
        ];
        var staticRates = ["$320 / night", "$450 / night", "$280 / night", "$195 / night", "$260 / night", "$340 / night"];
        var staticBadges = ["★ TRUSTED", "✦ WELLNESS", "✓ VERIFIED", "FREE", "✓ VERIFIED", "★ TRUSTED"];
        var cards = Array.prototype.slice.call(document.querySelectorAll("article"));
        replaceExactText("6 stays", properties.length + " stays");
        properties.forEach(function (property, index) {
          var card = cards[index];
          replaceExactText(staticTitles[index], property.title, card);
          replaceExactText(staticLocations[index], formatPropertyLocation(property), card);
          replaceExactText(staticRates[index], formatMoney(property.nightlyRate, property.currency) + " / night", card);
          replaceExactText(staticBadges[index], formatBadge(property.badgeLevel, false), card);

          if (card) {
            var price = Array.prototype.find.call(card.querySelectorAll("strong"), function (node) {
              return compactText(node.textContent).charAt(0) === "$";
            });
            if (price) price.textContent = formatMoney(property.nightlyRate, property.currency);

            var badge = Array.prototype.find.call(card.querySelectorAll("span"), function (node) {
              return ["★ TRUSTED", "✦ WELLNESS", "✓ VERIFIED", "FREE"].indexOf(compactText(node.textContent)) !== -1;
            });
            if (badge) badge.textContent = formatBadge(property.badgeLevel, false);
          }
        });
      }

      if (resolvedScreenId === "PUB-01") {
        var homeTitles = ["Cliffside Retreat", "Sea Grape Cottage", "Uptown Loft", "Mist Ridge Cabin"];
        var homeLocations = [
          "Negril · Westmoreland parish",
          "Treasure Beach · St. Elizabeth",
          "Kingston · St. Andrew",
          "Blue Mountains · St. Andrew"
        ];
        var homeRates = ["$450 / night", "$410 / night", "$195 / night", "$230 / night"];
        properties.slice(0, homeTitles.length).forEach(function (property, index) {
          replaceExactText(homeTitles[index], property.title);
          replaceExactText(homeLocations[index], formatPropertyLocation(property));
          replaceExactText(homeRates[index], formatMoney(property.nightlyRate, property.currency) + " / night");
        });
      }

      if (resolvedScreenId === "PUB-04") {
        var property = selectedProperty(properties);
        replaceExactText("Seaview Villa", property.title);
        replaceContainingText("Ocho Rios · St. Ann parish, Jamaica · Hosted by Marcia", formatPropertyLocation(property) + ", " + property.country + " · Hosted by " + property.hostName + " · ★ 4.9 (32 reviews)");
        replaceExactText("★ TRUSTED HOST", formatBadge(property.badgeLevel, true));
        replaceExactText("$320 / night", formatMoney(property.nightlyRate, property.currency) + " / night");
        replaceContainingText("A three-bedroom villa", property.title + " is a backend-seeded NestyStay listing in " + formatPropertyLocation(property) + ". Highlights include " + (property.highlights || []).slice(0, 3).join(", ") + ".");
        replaceContainingText("Cancellation policy Moderate", "Cancellation policy " + property.cancellationPolicy);
        replaceContainingText("Hosted by Marcia", "Hosted by " + property.hostName);
        var quote = await quoteFor(property);
        hydrateQuoteText(quote);
      }
    } catch (error) {
      addLiveBadge("API offline");
    }
  }

  async function hydrateBookingScreens() {
    if (resolvedScreenId.indexOf("BOOK-") !== 0) return;

    try {
      if (resolvedScreenId === "BOOK-07" || resolvedScreenId === "BOOK-CONF") {
        var bookingId = bookingIdFromPath();
        var session = getSession();
        if (bookingId && session && session.accessToken) {
          var booking = await api("/bookings/" + bookingId, { token: true });
          addLiveBadge("Booking " + booking.status + " · " + formatMoney(booking.totalAmount, booking.currency));
          replaceExactText("Cliffside Retreat", booking.propertyTitle || "Booked stay");
          replaceExactText("$2,970", formatMoney(booking.totalAmount, booking.currency));
          replaceContainingText("Payment authorized — $2,970", "Payment " + String(booking.paymentStatus || "authorized").toLowerCase() + " — " + formatMoney(booking.totalAmount, booking.currency));
          replaceContainingText("DATES HELD", booking.holdExpiresAt ? "DATES HELD UNTIL " + new Date(booking.holdExpiresAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) : "BOOKING " + String(booking.status || "pending").toUpperCase());
        }
        return;
      }

      var properties = await api("/properties");
      if (!Array.isArray(properties) || properties.length === 0) return;
      var property = selectedProperty(properties);
      saveBookingDraft(property);
      var quote = await quoteFor(property);
      addLiveBadge("Live quote · " + formatMoney(quote.totalAmount, quote.currency));
      replaceContainingText("Cliffside Retreat Negril", property.title + " · " + formatPropertyLocation(property) + " · ★ 4.9 " + formatBadge(property.badgeLevel, true));
      replaceExactText("Cliffside Retreat", property.title);
      replaceExactText("Negril · Westmoreland · ★ 4.9", formatPropertyLocation(property).replace(" parish", "") + " · ★ 4.9");
      replaceExactText("◆ WELLNESS HOST", formatBadge(property.badgeLevel, true));
      hydrateQuoteText(quote);

      var continueQuote = findAction(document, "Continue to quote");
      if (continueQuote) {
        continueQuote.addEventListener("click", function (event) {
          event.preventDefault();
          sendRoute("/booking/" + property.id + "/quote");
        });
      }

      var continueIdentity = findAction(document, "Continue to identity");
      if (continueIdentity) {
        continueIdentity.addEventListener("click", function (event) {
          event.preventDefault();
          sendRoute("/booking/" + property.id + "/identity");
        });
      }

      var continuePayment = findAction(document, "Continue to payment") || findAction(document, "Hold dates");
      if (continuePayment) {
        continuePayment.addEventListener("click", function (event) {
          event.preventDefault();
          sendRoute("/booking/" + property.id + "/checkout");
        });
      }

      var authorize = findAction(document, "Authorize");
      if (authorize) {
        authorize.textContent = "Authorize " + formatMoney(quote.totalAmount, quote.currency) + " →";
        authorize.addEventListener("click", async function (event) {
          event.preventDefault();
          var session = getSession();
          if (!session || !session.accessToken) {
            sendRoute("/401");
            return;
          }
          var draft = bookingDraftFor(property);
          setBusy(authorize, true, "Authorizing...");
          try {
            var booking = await api("/bookings", {
              method: "POST",
              token: true,
              body: {
                propertyId: property.id,
                checkIn: draft.checkIn,
                checkOut: draft.checkOut,
                adults: draft.adults || 2,
                children: draft.children || 0,
                billingCountry: "JM",
                termsAccepted: true,
                documentType: "Passport",
                ekycMetaInfo: "Imported design checkout"
              }
            });
            localStorage.setItem("nestyStay.lastBookingId", booking.id);
            sendRoute(booking.requiresGuestVerification ? "/booking/" + booking.id + "/pending" : "/booking/" + booking.id + "/success");
          } catch (error) {
            setStatus(document.body, error.message, "error");
          } finally {
            setBusy(authorize, false);
          }
        });
      }
    } catch (error) {
      addLiveBadge("Quote API offline");
    }
  }

  async function hydrateTravelerScreens() {
    var travelerScreens = ["TRAV-01", "TRAV-COL", "TRAV-INV", "TRAV-PEND", "TRAV-NOTIF", "TRAV-SUGG", "TRAV-12", "MSG-01", "MSG-DOC"];
    if (travelerScreens.indexOf(resolvedScreenId) === -1) return;

    var session = getSession();
    if (!session || !session.accessToken) return;

    try {
      var bookings = await api("/bookings", { token: true });
      var properties = await api("/properties");
      var total = Array.isArray(bookings)
        ? bookings.reduce(function (sum, booking) { return sum + (Number(booking.totalAmount) || 0); }, 0)
        : 0;
      addLiveBadge("Live traveler · " + (bookings.length || 0) + " bookings");
      replaceExactText("TOTAL SPENT", "TOTAL BOOKED");
      replaceExactText("$11,430", formatMoney(total, "USD"));
      replaceExactText("UPCOMING 2", "UPCOMING " + bookings.length);

      if (bookings[0]) {
        var property = Array.isArray(properties) ? properties.find(function (item) { return item.id === bookings[0].propertyId; }) : null;
        var stayLine = [
          property ? formatPropertyLocation(property).replace(" parish", "") : "Jamaica",
          formatStayRange(bookings[0].checkIn, bookings[0].checkOut),
          formatMoney(bookings[0].totalAmount, bookings[0].currency)
        ].join(" · ");
        replaceExactText("Cliffside Retreat", bookings[0].propertyTitle || "Booked stay");
        replaceContainingText("NST-2026-0148", "Booking " + String(bookings[0].id).slice(0, 8).toUpperCase());
        replaceContainingText("Negril · Westmoreland · Dec 12", stayLine);
        replaceExactText("BOOKING: CONFIRMED", "BOOKING: " + String(bookings[0].status || "pending").toUpperCase());
        replaceExactText("VERIFICATION: PASSED", "VERIFICATION: " + String(bookings[0].verificationStatus || "pending").toUpperCase());
        replaceExactText("PAYMENT: CAPTURED", "PAYMENT: " + String(bookings[0].paymentStatus || "pending").toUpperCase());
        replaceContainingText("Payment captured — $2,970", "Payment " + String(bookings[0].paymentStatus || "pending").toLowerCase() + " — " + formatMoney(bookings[0].totalAmount, bookings[0].currency));
      }

      try {
        var workspace = await api("/spec/traveler/" + session.userId, { token: true });
        if (workspace) {
          if (workspace.notifications && workspace.notifications[0]) {
            replaceExactText("Notifications", "Notifications (" + workspace.notifications.filter(function (item) { return !item.isRead; }).length + ")");
          }
        }
      } catch (error) {
        // Some freshly-created local users do not have a spec workspace yet; bookings remain the source of truth.
      }
    } catch (error) {
      addLiveBadge("Traveler API offline");
    }
  }

  async function hydrateHostScreens() {
    var hostScreens = ["HOST-01", "HOST-05", "HOST-EDIT", "HOST-RPT", "HOST-WELL", "HOST-BADGE"];
    if (hostScreens.indexOf(resolvedScreenId) === -1) return;

    var session = getSession();
    if (!session || !session.accessToken) return;

    try {
      var properties = await api("/properties");
      var hostProperties = properties.filter(function (property) {
        return property.hostUserId === session.userId || (session.roles || []).indexOf("Admin") !== -1;
      });
      var visibleProperties = hostProperties.length ? hostProperties : properties;
      addLiveBadge("Live host · " + visibleProperties.length + " listings");
      replaceMetricValue("PROPERTIES", visibleProperties.length);

      var hostTitles = ["Cliffside Retreat", "Sea Grape Cottage", "Uptown Loft"];
      var hostLocations = ["Negril · Westmoreland", "Treasure Beach · St. Elizabeth", "Kingston · St. Andrew"];
      var hostRates = ["$450/night", "$160/night", "$210/night"];
      visibleProperties.slice(0, hostTitles.length).forEach(function (property, index) {
        var titleNode = leafNodes().find(function (node) {
          return node.children.length === 0 && compactText(node.textContent) === hostTitles[index];
        });
        var row = titleNode && titleNode.closest ? titleNode.closest("div[style*='gap:14px']") : null;
        replaceExactText(hostTitles[index], property.title, row);
        replaceContainingText(hostLocations[index], formatPropertyLocation(property).replace(" parish", "") + " · " + formatMoney(property.nightlyRate, property.currency) + "/night", row);
        replaceExactText(hostRates[index], formatMoney(property.nightlyRate, property.currency) + "/night", row);
      });

      if ((session.roles || []).indexOf("Host") !== -1) {
        try {
          var operations = await api("/spec/host/" + session.userId + "/operations", { token: true });
          if (operations && operations.analytics) {
            replaceMetricValue("BOOKINGS — DEC", operations.analytics.bookingCount);
            replaceMetricValue("REVENUE — DEC", formatMoney(operations.analytics.revenue, "USD"));
            addLiveBadge("Live host · " + operations.analytics.bookingCount + " bookings");
          }
        } catch (error) {
          // Host operation seed data may not exist for every local test host.
        }
      }
    } catch (error) {
      addLiveBadge("Host API offline");
    }
  }

  async function hydrateAdminScreens() {
    var adminScreens = ["ADM-01", "ADM-KPI", "ADM-RPT", "ADM-RESET"];
    if (adminScreens.indexOf(resolvedScreenId) === -1) return;

    var session = getSession();
    if (!session || !session.accessToken) return;

    try {
      var properties = await api("/properties");
      var bookings = await api("/bookings", { token: true });
      var gmv = Array.isArray(bookings)
        ? bookings.reduce(function (sum, booking) { return sum + (Number(booking.totalAmount) || 0); }, 0)
        : 0;
      addLiveBadge("Live admin · " + properties.length + " listings");
      replaceMetricValue("ACTIVE LISTINGS", properties.length);
      replaceMetricValue("BOOKINGS — 7D", bookings.length || 0);
      replaceMetricValue("GMV — 7D", formatMoney(gmv, "USD"));

      try {
        var pricebook = await api("/badges-pricing/pricebook");
        replaceExactText("Pricebook — 16 items", "Pricebook — " + pricebook.length + " items");
        var priceItems = [
          ["Service fee tier 1", "12 %", "guest-standard-fee-short"],
          ["Service fee tier 2", "10 %", "guest-standard-fee-mid"],
          ["Service fee tier 3", "8 %", "guest-standard-fee-low"],
          ["Traveler eKYC (first)", "$9.99", "guest-ekyc-first-html"],
          ["Traveler eKYC (return)", "$4.99", "guest-ekyc-return-html"],
          ["Trusted badge (annual)", "$120.00", "trusted-host-standard-annual"]
        ];
        priceItems.forEach(function (row) {
          var item = pricebook.find(function (entry) { return entry.key === row[2]; });
          if (!item) return;
          replaceExactText(row[0], item.label);
          replaceExactText(row[1], item.currency === "PERCENT" ? item.amount + " %" : formatMoney(item.amount, item.currency));
        });
      } catch (error) {
        // Pricebook is public in local mode, but the dashboard can still run without it.
      }

      try {
        var operations = await api("/spec/admin/operations", { token: true });
        if (operations && operations.metrics) {
          addLiveBadge("Live admin · " + operations.metrics.length + " metrics");
        }
      } catch (error) {
        // Admin operation fixtures are optional for dashboard validation.
      }
    } catch (error) {
      addLiveBadge("Admin API offline");
    }
  }

  function saveSession(session) {
    localStorage.setItem(sessionStorageKey, JSON.stringify(session));
    window.parent.postMessage({ type: "nesty-session-saved" }, "*");
  }

  function sessionFromAuth(response, fallback) {
    return {
      userId: response.userId,
      email: response.email || fallback.email,
      displayName: response.displayName || fallback.displayName || fallback.email,
      accessToken: response.accessToken,
      expiresAt: response.expiresAt,
      roles: response.roles || [],
      permissions: response.permissions || []
    };
  }

  function dashboardFor(session) {
    var roles = (session.roles || []).map(function (role) { return String(role).toLowerCase(); });
    if (roles.indexOf("admin") !== -1) return "/admin/ops/disputes";
    if (roles.indexOf("host") !== -1) return "/host-dashboard";
    if (roles.indexOf("propertymanager") !== -1) return "/pm/gates";
    if (roles.indexOf("officer") !== -1) return "/officer/wellness";
    return "/auth/post-login-toast";
  }

  function fillTwoFactorDigits(code) {
    var digits = String(code || "").replace(/\\D/g, "").slice(0, 6).split("");
    var twoFactorSection = document.getElementById("2fa");
    Array.prototype.forEach.call(twoFactorSection ? twoFactorSection.querySelectorAll("input") : [], function (input, index) {
      input.value = digits[index] || "";
    });
  }

  async function fillDevelopmentTwoFactorCode(challengeId) {
    try {
      var challenge = await api("/auth/development/challenges/" + encodeURIComponent(challengeId));
      if (challenge && challenge.code) {
        fillTwoFactorDigits(challenge.code);
        return true;
      }
    } catch (error) {
      return false;
    }
    return false;
  }

  function hidePrototypeErrors(root) {
    Array.prototype.forEach.call((root || document).querySelectorAll("div,p,span"), function (node) {
      if (compactText(node.textContent) === "Invalid email or password.") {
        node.style.display = "none";
      }
    });
  }

  function wireAuthScreen() {
    var loginSection = findSectionByHeading("Log in");
    var twoFactorSection = document.getElementById("2fa");
    var registerSection = document.getElementById("register");
    var recoverSection = document.getElementById("recover");
    var pendingChallenge = null;
    var lastLogin = null;

    hidePrototypeErrors(loginSection);

    var loginPassword = fieldByHint(loginSection, ["password"], "password");
    if (loginPassword && /\\u2022/.test(loginPassword.value)) {
      loginPassword.value = "";
      loginPassword.placeholder = "Password";
    }

    var registerPassword = fieldByHint(registerSection, ["password"], "password");
    if (registerPassword && registerPassword.value === "Nesty2026") {
      registerPassword.value = "";
      registerPassword.placeholder = "Password";
    }

    Array.prototype.forEach.call(twoFactorSection ? twoFactorSection.querySelectorAll("input") : [], function (input) {
      input.value = "";
      input.maxLength = 1;
      input.inputMode = "numeric";
      input.addEventListener("input", function () {
        input.value = input.value.replace(/\\D/g, "").slice(0, 1);
        if (input.value && input.nextElementSibling && input.nextElementSibling.tagName === "INPUT") {
          input.nextElementSibling.focus();
        }
      });
    });

    async function continueAfterLogin(login, fallback) {
      if (login.requiresTwoFactor) {
        pendingChallenge = {
          challengeId: login.challengeId,
          email: login.email || fallback.email,
          displayName: fallback.displayName || fallback.email
        };
        var filled = await fillDevelopmentTwoFactorCode(login.challengeId);
        setStatus(
          twoFactorSection,
          filled ? "2FA challenge ready. Development code filled from the backend." : "2FA challenge ready. Enter the code from the backend.",
          "success"
        );
        notifyAnchor(twoFactorSection);
        return;
      }

      var session = sessionFromAuth(login, fallback);
      saveSession(session);
      sendRoute(dashboardFor(session));
    }

    var loginButton = findAction(loginSection, "Log in");
    if (loginButton) {
      loginButton.addEventListener("click", async function (event) {
        event.preventDefault();
        var emailInput = fieldByHint(loginSection, ["email"], "email");
        var passwordInput = fieldByHint(loginSection, ["password"], "password");
        var email = emailInput && emailInput.value.trim();
        var password = passwordInput && passwordInput.value;
        if (!email || !password) {
          setStatus(loginSection, "Enter your email and password to log in.", "error");
          return;
        }

        lastLogin = { email: email, password: password, displayName: email };
        setBusy(loginButton, true, "Logging in...");
        try {
          var login = await api("/auth/login", { method: "POST", body: { email: email, password: password } });
          await continueAfterLogin(login, lastLogin);
        } catch (error) {
          setStatus(loginSection, error.message, "error");
        } finally {
          setBusy(loginButton, false);
        }
      });
    }

    var verifyButton = findAction(twoFactorSection, "Verify code");
    if (verifyButton) {
      verifyButton.addEventListener("click", async function (event) {
        event.preventDefault();
        if (!pendingChallenge) {
          setStatus(twoFactorSection, "Start by logging in so the backend can create a 2FA challenge.", "error");
          return;
        }
        await fillDevelopmentTwoFactorCode(pendingChallenge.challengeId);
        var code = Array.prototype.map.call(twoFactorSection.querySelectorAll("input"), function (input) {
          return input.value;
        }).join("");
        if (code.length !== 6) {
          setStatus(twoFactorSection, "Enter the 6-digit verification code.", "error");
          return;
        }

        setBusy(verifyButton, true, "Verifying...");
        try {
          var verified = await api("/auth/2fa/verify", {
            method: "POST",
            body: { challengeId: pendingChallenge.challengeId, code: code }
          });
          var session = sessionFromAuth(verified, pendingChallenge);
          saveSession(session);
          sendRoute(dashboardFor(session));
        } catch (error) {
          setStatus(twoFactorSection, error.message, "error");
        } finally {
          setBusy(verifyButton, false);
        }
      });
    }

    var resendButton = findAction(twoFactorSection, "Resend code");
    if (resendButton) {
      resendButton.addEventListener("click", async function (event) {
        event.preventDefault();
        if (!lastLogin) {
          setStatus(twoFactorSection, "Log in again so the backend can send a fresh code.", "error");
          return;
        }

        setBusy(resendButton, true, "Sending...");
        try {
          var login = await api("/auth/login", { method: "POST", body: { email: lastLogin.email, password: lastLogin.password } });
          await continueAfterLogin(login, lastLogin);
        } catch (error) {
          setStatus(twoFactorSection, error.message, "error");
        } finally {
          setBusy(resendButton, false);
        }
      });
    }

    var registerButton = findAction(registerSection, "Create my account");
    if (registerButton) {
      registerButton.addEventListener("click", async function (event) {
        event.preventDefault();
        var fields = Array.prototype.slice.call(registerSection.querySelectorAll("input"));
        var firstName = fields[0] && fields[0].value.trim();
        var lastName = fields[1] && fields[1].value.trim();
        var emailInput = fieldByHint(registerSection, ["email"], "email");
        var passwordInput = fieldByHint(registerSection, ["password"], "password");
        var email = emailInput && emailInput.value.trim();
        var password = passwordInput && passwordInput.value;
        var displayName = compactText([firstName, lastName].filter(Boolean).join(" ")) || email;
        if (!email || !password || !displayName) {
          setStatus(registerSection, "Enter your name, email, and password to create the account.", "error");
          return;
        }

        setBusy(registerButton, true, "Creating...");
        try {
          await api("/auth/register", {
            method: "POST",
            body: {
              email: email,
              password: password,
              confirmPassword: password,
              displayName: displayName,
              acceptedTerms: true,
              acceptedPrivacy: true,
              role: "Guest"
            }
          });
          var login = await api("/auth/login", { method: "POST", body: { email: email, password: password } });
          lastLogin = { email: email, password: password, displayName: displayName };
          await continueAfterLogin(login, lastLogin);
        } catch (error) {
          setStatus(registerSection, error.message, "error");
        } finally {
          setBusy(registerButton, false);
        }
      });
    }

    var recoverButton = findAction(recoverSection, "Send reset link");
    if (recoverButton) {
      recoverButton.addEventListener("click", async function (event) {
        event.preventDefault();
        var emailInput = fieldByHint(recoverSection, ["email"], "email");
        var email = emailInput && emailInput.value.trim();
        if (!email) {
          setStatus(recoverSection, "Enter the account email first.", "error");
          return;
        }

        setBusy(recoverButton, true, "Sending...");
        try {
          var result = await api("/auth/password-reset/request", { method: "POST", body: { email: email } });
          setStatus(recoverSection, (result && result.message) || "Reset instructions were sent if the account exists.", "success");
        } catch (error) {
          setStatus(recoverSection, error.message, "error");
        } finally {
          setBusy(recoverButton, false);
        }
      });
    }

    var googleButton = findAction(loginSection, "Continue with Google");
    if (googleButton) {
      googleButton.addEventListener("click", function (event) {
        event.preventDefault();
        setStatus(loginSection, "Google sign-in is available after the OAuth client ID is configured.", "error");
      });
    }
  }

  document.addEventListener("click", function (event) {
    if (event.defaultPrevented) return;
    var target = event.target;
    var anchor = target && target.closest ? target.closest("a[href]") : null;
    if (!anchor) return;

    var href = anchor.getAttribute("href") || "";
    if (!href || href.charAt(0) !== "/") return;

    event.preventDefault();
    sendRoute(href);
  });

  window.addEventListener("load", function () {
    if (resolvedScreenId === "AUTH-01") wireAuthScreen();
    hydratePublicScreens();
    hydrateBookingScreens();
    hydrateTravelerScreens();
    hydrateHostScreens();
    hydrateAdminScreens();
    notifyHeight();
    notifyAnchor();
  });
  window.addEventListener("resize", notifyHeight);
  if (window.ResizeObserver && document.documentElement) {
    new ResizeObserver(notifyHeight).observe(document.documentElement);
  }
  setTimeout(notifyHeight, 60);
  setTimeout(notifyHeight, 400);
  setTimeout(notifyHeight, 1200);
  setTimeout(notifyAnchor, 120);
  setTimeout(notifyAnchor, 600);
})();
</script>`;

  const prepared = rewriteLinks(html)
    .replace(/<script\s+src=(["'])\.\/support\.js\1><\/script>/gi, "")
    .replace(/uploads\/pasted-1784678954416-0\.png/g, LOGO_URL);

  if (prepared.includes("</body>")) {
    return prepared.replace("</body>", `${bridge}</body>`);
  }

  return `${prepared}${bridge}`;
}

export function ImportedDesignScreen({ screenId }: { screenId: string }) {
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const [screens, setScreens] = useState<Record<string, string> | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [frameHeight, setFrameHeight] = useState("100vh");

  const [rawScreenId] = screenId.split("#");
  const resolvedScreenId = screenAliases[rawScreenId] ?? rawScreenId;

  useEffect(() => {
    let alive = true;

    loadScreens()
      .then((loadedScreens) => {
        if (alive) setScreens(loadedScreens);
      })
      .catch((loadError: Error) => {
        if (alive) setError(loadError.message);
      });

    return () => {
      alive = false;
    };
  }, []);

  useEffect(() => {
    const onMessage = (event: MessageEvent) => {
      if (event.source !== iframeRef.current?.contentWindow) return;

      const data = event.data as { type?: string; href?: string; height?: number; screenId?: string; top?: number };
      if (data.type === "nesty-route" && data.href?.startsWith("/")) {
        window.history.pushState({}, "", data.href);
        window.dispatchEvent(new PopStateEvent("popstate"));
        window.scrollTo({ top: 0, behavior: "auto" });
      }

      if (data.type === "nesty-session-saved") {
        window.dispatchEvent(new Event("storage"));
      }

      if (data.type === "nesty-screen-height" && data.screenId === resolvedScreenId && typeof data.height === "number") {
        setFrameHeight(`${Math.max(data.height, window.innerHeight)}px`);
      }

      if (data.type === "nesty-screen-anchor" && data.screenId === resolvedScreenId && typeof data.top === "number") {
        window.scrollTo({ top: data.top, behavior: "auto" });
      }
    };

    window.addEventListener("message", onMessage);
    return () => window.removeEventListener("message", onMessage);
  }, [resolvedScreenId]);

  const screenHtml = screens?.[resolvedScreenId];
  const currentPath = window.location.pathname;
  const srcDoc = useMemo(() => {
    if (!screenHtml) return undefined;
    return prepareScreenHtml(screenId, screenHtml, API_BASE_URL, currentPath);
  }, [currentPath, screenId, screenHtml]);

  if (error) {
    return (
      <main className="imported-design-status" role="main">
        <h1>Design screen could not load</h1>
        <p>{error}</p>
      </main>
    );
  }

  if (!screens) {
    return (
      <main className="imported-design-status" role="main">
        <h1>Loading NestyStay screen</h1>
      </main>
    );
  }

  if (!screenHtml || !srcDoc) {
    return (
      <main className="imported-design-status" role="main">
        <h1>Screen not found</h1>
        <p>{resolvedScreenId}</p>
      </main>
    );
  }

  return (
    <main className="imported-design-main" role="main" data-screen-id={resolvedScreenId}>
      <iframe
        ref={iframeRef}
        className="imported-design-frame"
        title={`NestyStay ${resolvedScreenId}`}
        srcDoc={srcDoc}
        style={{ height: frameHeight }}
        onLoad={() => {
          const iframe = iframeRef.current;
          const contentDocument = iframe?.contentDocument;
          if (!contentDocument) return;

          const height = Math.max(
            contentDocument.body?.scrollHeight ?? 0,
            contentDocument.documentElement?.scrollHeight ?? 0,
            window.innerHeight,
          );
          setFrameHeight(`${height}px`);
        }}
      />
    </main>
  );
}
