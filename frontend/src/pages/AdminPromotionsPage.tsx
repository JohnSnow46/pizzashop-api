import { useState } from 'react'
import { PromotionForm } from '../components/admin/PromotionForm'
import { PromotionList } from '../components/admin/PromotionList'
import type { Promotion } from '../api/types'
import { usePromotions } from '../hooks/usePromotions'

type PromotionFormState = { mode: 'closed' } | { mode: 'create' } | { mode: 'edit'; promotion: Promotion }

/**
 * `/admin/promotions` (RequireAuth roles=RestaurantAdmin/SuperAdmin, mirrors AuthRoles.Admin) —
 * promotion management: list + create/edit form. Edit is deliberately narrower than create
 * (ADR-0019, see PromotionForm) — only isActive, the valid-from/to window, value and usageLimit
 * are mutable after creation.
 */
export function AdminPromotionsPage() {
  const { promotions, loading, error, reload } = usePromotions()
  const [form, setForm] = useState<PromotionFormState>({ mode: 'closed' })

  function handleSaved() {
    setForm({ mode: 'closed' })
    reload()
  }

  return (
    <div className="admin-page">
      <h2>Panel admina — promocje</h2>

      <section className="admin-section">
        <h3>Promocje</h3>

        {loading && <p>Ładowanie promocji...</p>}
        {error && <p className="empty-state">{error}</p>}

        {!loading && !error && (
          <PromotionList promotions={promotions} onEdit={(promotion) => setForm({ mode: 'edit', promotion })} />
        )}

        {form.mode === 'closed' && (
          <button type="button" onClick={() => setForm({ mode: 'create' })}>
            Dodaj nową promocję
          </button>
        )}

        {form.mode === 'create' && (
          <PromotionForm mode="create" onSaved={handleSaved} onCancel={() => setForm({ mode: 'closed' })} />
        )}

        {form.mode === 'edit' && (
          <PromotionForm
            mode="edit"
            promotion={form.promotion}
            onSaved={handleSaved}
            onCancel={() => setForm({ mode: 'closed' })}
          />
        )}
      </section>
    </div>
  )
}
