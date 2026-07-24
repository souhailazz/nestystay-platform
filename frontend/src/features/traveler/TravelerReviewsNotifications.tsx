import { useState, useEffect } from "react";
import { Star, MessageSquare, Bell, Check, Edit2, Clock } from "lucide-react";
import { api } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import type { ReviewItem, NotificationItem } from "./types";

interface TravelerReviewsNotificationsProps {
  view: string;
  userId: string;
  token: string;
}

export function TravelerReviewsNotifications({ view, userId, token }: TravelerReviewsNotificationsProps) {
  const [reviews, setReviews] = useState<ReviewItem[]>([
    {
      id: "rev-1",
      bookingId: "b-100",
      propertyTitle: "Ocho Rios Verified Villa",
      rating: 5,
      comment: "Absolutely amazing stay! The host was super friendly and the pool view was breathtaking.",
      createdAt: new Date().toISOString(),
      hostReply: "Thanks for staying with us! Hope to see you back in Ocho Rios soon!",
      canEdit: true
    }
  ]);

  const [notifications, setNotifications] = useState<NotificationItem[]>([
    {
      id: "notif-1",
      title: "Booking Approved!",
      message: "Host Island Villa Hosting approved your booking request.",
      isRead: false,
      createdAt: new Date().toISOString()
    },
    {
      id: "notif-2",
      title: "Payment Receipt Available",
      message: "Your payment receipt for $585.00 USD is now ready for download.",
      isRead: true,
      createdAt: new Date().toISOString()
    }
  ]);

  const [editingReviewId, setEditingReviewId] = useState<string | null>(null);
  const [editComment, setEditComment] = useState("");

  function handleMarkAllRead() {
    setNotifications(notifications.map(n => ({ ...n, isRead: true })));
  }

  function handleUpdateReview(id: string) {
    setReviews(reviews.map(r => r.id === id ? { ...r, comment: editComment } : r));
    setEditingReviewId(null);
  }

  if (view === "notifications") {
    return (
      <div className="page-container container py-6" data-testid="trav-16-notifications" id="TRAV-16">
        <header className="page-header mb-6 flex justify-between items-center">
          <div>
            <span className="badge badge-sun">TRAV-16</span>
            <h2>Notifications Center</h2>
            <PatoisPhrase phrase="Stay In The Loop" translation="All your real-time alerts, payment updates, and host messages." />
          </div>
          <button type="button" className="btn btn-outline" onClick={handleMarkAllRead}>
            <Check size={16} /> Mark All as Read
          </button>
        </header>

        <div className="space-y-3 max-w-2xl">
          {notifications.map((n) => (
            <div key={n.id} className={`card-box ${n.isRead ? "bg-white" : "bg-sun-light border-sun"}`}>
              <div className="flex justify-between items-start">
                <div>
                  <strong className="text-lg">{n.title}</strong>
                  <p className="subtext mt-1">{n.message}</p>
                </div>
                <span className="text-xs subtext">{new Date(n.createdAt).toLocaleTimeString()}</span>
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="page-container container py-6" data-testid="trav-15-reviews" id="TRAV-15">
      <header className="page-header mb-6">
        <span className="badge badge-sun">TRAV-15 / TRAV-16</span>
        <h2>My Reviews & Feedback</h2>
        <PatoisPhrase phrase="Share Yuh Experience" translation="Submit and manage reviews for your completed stays." />
      </header>

      <div className="space-y-6 max-w-3xl">
        {reviews.map((rev) => (
          <div key={rev.id} className="card-box">
            <div className="flex justify-between items-center mb-2">
              <h3 className="font-bold text-lg">{rev.propertyTitle}</h3>
              <div className="flex items-center gap-1 text-sun">
                {[...Array(rev.rating)].map((_, i) => (
                  <Star key={i} size={16} fill="currentColor" />
                ))}
              </div>
            </div>

            {editingReviewId === rev.id ? (
              <div className="my-3">
                <textarea 
                  className="input-control mb-2" 
                  value={editComment} 
                  onChange={(e) => setEditComment(e.target.value)} 
                />
                <div className="flex gap-2">
                  <button type="button" className="btn btn-primary btn-sm" onClick={() => handleUpdateReview(rev.id)}>Save Edit</button>
                  <button type="button" className="btn btn-ghost btn-sm" onClick={() => setEditingReviewId(null)}>Cancel</button>
                </div>
              </div>
            ) : (
              <p className="my-2">{rev.comment}</p>
            )}

            {rev.hostReply && (
              <div className="host-reply-box bg-gray-50 p-3 rounded mt-3 border-l-4 border-sun">
                <strong className="text-sm">Host Reply:</strong>
                <p className="subtext text-sm mt-1">{rev.hostReply}</p>
              </div>
            )}

            {rev.canEdit && editingReviewId !== rev.id && (
              <button 
                type="button" 
                className="btn btn-ghost btn-sm mt-3"
                onClick={() => { setEditingReviewId(rev.id); setEditComment(rev.comment); }}
              >
                <Edit2 size={14} /> Edit Review (Within 48h Window)
              </button>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
