import { useEffect, useState } from 'react'
import { ApiError } from '../../api/client'
import { createReview, getMyReviews } from '../../api/reviewsApi'
import type { Order, Review } from '../../api/types'

const RATINGS = [1, 2, 3, 4, 5]

/** Mirrors PizzaShop.Domain.Reviews.Review.MaxCommentLength — the comment textarea had no
 * client-side cap at all, so a comment over the backend's limit passed the "is there a
 * rating" check here and only failed on a bare 400 from CreateReviewCommandValidator. */
const MAX_COMMENT_LENGTH = 1000

interface ReviewItemRowProps {
  orderId: string
  menuItemId: string
  menuItemName: string
  onReviewed: (review: Review) => void
}

function ReviewItemRow({ orderId, menuItemId, menuItemName, onReviewed }: ReviewItemRowProps) {
  const [rating, setRating] = useState<number | null>(null)
  const [comment, setComment] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit() {
    if (!rating) {
      setError('Wybierz ocenę.')
      return
    }

    setSubmitting(true)
    setError(null)
    try {
      const id = await createReview({ orderId, menuItemId, rating, comment: comment.trim() || null })
      onReviewed({ id, orderId, customerId: '', menuItemId, rating, comment: comment.trim() || null, createdAt: new Date().toISOString() })
    } catch (err: unknown) {
      setError(err instanceof ApiError ? err.message : 'Nie udało się zapisać recenzji.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <li className="order-review-form__item">
      <p>{menuItemName}</p>
      <div className="order-review-form__ratings" role="radiogroup" aria-label={`Ocena dla ${menuItemName}`}>
        {RATINGS.map((value) => (
          <button
            key={value}
            type="button"
            className={`order-review-form__rating${rating === value ? ' order-review-form__rating--selected' : ''}`}
            aria-pressed={rating === value}
            onClick={() => setRating(value)}
          >
            {value}★
          </button>
        ))}
      </div>
      <textarea
        className="order-review-form__comment"
        placeholder="Komentarz (opcjonalnie)"
        value={comment}
        onChange={(event) => setComment(event.target.value)}
        maxLength={MAX_COMMENT_LENGTH}
      />
      {error && <p className="checkout-error">{error}</p>}
      <button type="button" className="add-to-cart-btn" disabled={submitting} onClick={handleSubmit}>
        {submitting ? 'Zapisywanie…' : 'Wyślij recenzję'}
      </button>
    </li>
  )
}

/**
 * Review form for a completed order's items (ADR-0042). Only rendered by the caller once
 * `order.status === 'Completed'` — the backend enforces the same rule (CreateReviewCommandHandler),
 * this just avoids showing a form that would 409 on submit.
 */
export function OrderReviewForm({ order }: { order: Order }) {
  const [myReviews, setMyReviews] = useState<Review[] | null>(null)

  useEffect(() => {
    let cancelled = false
    getMyReviews()
      .then((reviews) => {
        if (!cancelled) setMyReviews(reviews)
      })
      .catch(() => {
        if (!cancelled) setMyReviews([])
      })
    return () => {
      cancelled = true
    }
  }, [])

  if (myReviews === null) {
    return <p className="checkout-hint">Ładowanie…</p>
  }

  const reviewedMenuItemIds = new Set(
    myReviews.filter((review) => review.orderId === order.id).map((review) => review.menuItemId),
  )

  // One row per distinct menu item in the order — reviewing is per (order, menu item),
  // not per line, so a duplicate line (e.g. ordered twice) only needs one form.
  const uniqueItems = [...new Map(order.items.map((item) => [item.menuItemId, item])).values()]
  const pendingItems = uniqueItems.filter((item) => !reviewedMenuItemIds.has(item.menuItemId))

  if (pendingItems.length === 0) {
    return <p className="checkout-hint">Dziękujemy za ocenienie tego zamówienia!</p>
  }

  return (
    <div className="order-review-form">
      <h3>Oceń zamówione pozycje</h3>
      <ul className="order-review-form__list">
        {pendingItems.map((item) => (
          <ReviewItemRow
            key={item.menuItemId}
            orderId={order.id}
            menuItemId={item.menuItemId}
            menuItemName={item.menuItemName}
            onReviewed={(review) => setMyReviews((prev) => [...(prev ?? []), review])}
          />
        ))}
      </ul>
    </div>
  )
}
