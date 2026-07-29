/**
 * @vitest-environment jsdom
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { BookingStateContainer } from './BookingStateContainer';
import { BookingCheckoutPage } from './BookingCheckoutPage';
import { BookingReviewPage } from './BookingReviewPage';
import { api } from '../../lib/api';

vi.mock('../../lib/api', () => ({
  api: {
    getProperties: vi.fn(),
    getBookingQuote: vi.fn(),
    getBooking: vi.fn(),
    createBooking: vi.fn(),
    downloadBookingInvoice: vi.fn(),
    downloadBookingReceipt: vi.fn(),
  },
  formatMoney: (amt: number, curr: string) => `${curr} ${amt}`,
}));

vi.mock('../../lib/patois', () => ({
  PatoisPhrase: ({ phrase }: any) => <span data-testid="patois-mock">{phrase}</span>,
  usePatois: () => ({ t: (s: string) => s })
}));

vi.mock('@stripe/react-stripe-js', () => ({
  Elements: ({ children }: any) => <div data-testid="stripe-elements">{children}</div>,
  PaymentElement: () => <div data-testid="payment-element" />,
  useStripe: () => ({
    confirmPayment: vi.fn(),
  }),
  useElements: () => ({
    submit: vi.fn().mockResolvedValue({ error: null }),
  }),
}));

vi.mock('@stripe/stripe-js', () => ({
  loadStripe: vi.fn().mockResolvedValue({}),
}));

const mockAuth = {
  session: {
    userId: 'guest-123',
    email: 'guest@example.com',
    displayName: 'Test Guest',
    accessToken: 'mock-token',
    refreshToken: 'mock-refresh',
    role: 'Guest',
    roles: ['Guest'],
    permissions: [],
    expiresAt: new Date(Date.now() + 86400000).toISOString()
  },
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
  requestPasswordReset: vi.fn(),
  completePasswordReset: vi.fn(),
  refreshSession: vi.fn()
};

describe('Booking Screens (BOOK-01 to BOOK-10)', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_STRIPE_PUBLIC_KEY', 'pk_test_frontend_unit');
    vi.clearAllMocks();
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllEnvs();
  });

  describe('BOOK-01 & BOOK-02: Guest Selection & Review', () => {
    it('renders the Booking Review page properly', async () => {
      const mockQuote = {
        property: { id: 'p1', title: 'Test Villa', location: 'Ocho Rios', country: 'JM', cancellationPolicy: 'Strict' },
        checkIn: '2026-08-01', checkOut: '2026-08-05', nights: 4, nightlyRate: 100, staySubtotal: 400,
        guestPlatformFee: 40, totalAmount: 440, currency: 'USD', requiresGuestVerification: false,
        datesAvailable: true, priceBreakdown: []
      };

      vi.mocked(api.getProperties).mockResolvedValue([{ id: 'p1' } as any]);
      vi.mocked(api.getBookingQuote).mockResolvedValue(mockQuote as any);

      render(<BookingStateContainer state="review" auth={mockAuth as any} />);

      await waitFor(() => {
        expect(screen.getByTestId('book-02-page')).toBeDefined();
      });
      
      expect(screen.getByText('Test Villa')).toBeDefined();
    });

    it('shows a human-readable retry message for booking rate limits', async () => {
      const mockQuote = {
        property: { id: 'p1', title: 'Test Villa', location: 'Ocho Rios', country: 'JM', cancellationPolicy: 'Strict' },
        checkIn: '2026-08-01', checkOut: '2026-08-05', nights: 4, nightlyRate: 100, staySubtotal: 400,
        guestPlatformFee: 40, totalAmount: 440, currency: 'USD', requiresGuestVerification: false,
        datesAvailable: true, priceBreakdown: []
      };

      vi.mocked(api.createBooking).mockRejectedValue(Object.assign(new Error('Too many booking requests.'), {
        status: 429,
        code: 'rate_limit_exceeded',
        retryAfterSeconds: 125,
      }));

      const { container } = render(
        <BookingReviewPage
          quote={mockQuote as any}
          details={{ adults: 2, children: 0, accessibility: '', protection: 'insuraguest' }}
          auth={mockAuth as any}
          onBackToModal={vi.fn()}
          onProceedToCheckout={vi.fn()}
        />
      );

      const termsCheckbox = container.querySelector('input[type="checkbox"]');
      expect(termsCheckbox).toBeTruthy();
      fireEvent.click(termsCheckbox as HTMLInputElement);
      fireEvent.click(within(container).getByRole('button', { name: /Proceed to Secure Checkout/i }));

      await waitFor(() => {
        expect(screen.getByText(/Please wait 3 minutes before trying again/i)).toBeDefined();
      });
    });
  });

  describe('BOOK-03: Checkout', () => {
    it('renders secure stripe checkout if clientSecret is present', async () => {
      vi.mocked(api.getBooking).mockResolvedValue({
        id: 'b1', totalAmount: 440, currency: 'USD', paymentClientSecret: 'pi_secret', status: 'Approved'
      } as any);

      render(<BookingCheckoutPage bookingId="b1" auth={mockAuth as any} onSuccess={vi.fn()} onFailure={vi.fn()} />);

      await waitFor(() => {
        expect(screen.getByTestId('book-03-page')).toBeDefined();
      });

      expect(screen.getByTestId('stripe-elements')).toBeDefined();
      expect(screen.getByTestId('payment-element')).toBeDefined();
    });

    it('shows configuration error if the Stripe publishable key is missing', async () => {
      vi.stubEnv('VITE_STRIPE_PUBLIC_KEY', '');
      vi.mocked(api.getBooking).mockResolvedValue({
        id: 'b1', totalAmount: 440, currency: 'USD', paymentClientSecret: 'pi_secret', status: 'Approved'
      } as any);

      render(<BookingCheckoutPage bookingId="b1" auth={mockAuth as any} onSuccess={vi.fn()} onFailure={vi.fn()} />);

      await waitFor(() => {
        expect(screen.getByTestId('book-03-stripe-config-missing')).toBeDefined();
      });

      expect(screen.queryByTestId('stripe-elements')).toBeNull();
    });
  });

  describe('BOOK-04 to BOOK-10: Post-Booking states', () => {
    const states = [
      { state: 'success', id: 'BOOK-04' },
      { state: 'failure', id: 'BOOK-05' },
      { state: 'rejected', id: 'BOOK-06' },
      { state: 'pending', id: 'BOOK-07' },
      { state: 'cancelled', id: 'BOOK-08' },
      { state: 'invoice', id: 'BOOK-09' },
      { state: 'receipt', id: 'BOOK-10' },
    ];

    states.forEach(({ state, id }) => {
      it(`renders ${id} (${state}) correctly`, async () => {
        vi.mocked(api.getBooking).mockResolvedValue({
          id: 'b1', totalAmount: 440, currency: 'USD', status: 'mock', timeline: [], priceBreakdown: []
        } as any);

        render(<BookingStateContainer state={state} bookingId="b1" auth={mockAuth as any} />);

        await waitFor(() => {
          expect(screen.getByTestId(`${id.toLowerCase()}-page`)).toBeDefined();
        });
      });
    });
  });
});
