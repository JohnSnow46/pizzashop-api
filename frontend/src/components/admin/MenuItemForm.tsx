import { useState, type ChangeEvent, type FormEvent } from 'react'
import { ApiError } from '../../api/client'
import { createMenuItem, updateMenuItem, uploadMenuItemImage } from '../../api/menuApi'
import type { Ingredient, MenuCategory, MenuItem, MenuItemVariantInput, UpdateMenuItemCommand } from '../../api/types'
import { MenuItemVariantsEditor } from './MenuItemVariantsEditor'

const MENU_CATEGORIES: MenuCategory[] = ['Pizza', 'Drink', 'Side', 'Dessert', 'Sauce']

interface MenuItemFormProps {
  mode: 'create' | 'edit'
  /** Required when mode === 'edit'. */
  item?: MenuItem
  ingredients: Ingredient[]
  onSaved: () => void
  onCancel: () => void
}

function toggleId(ids: string[], id: string): string[] {
  return ids.includes(id) ? ids.filter((existing) => existing !== id) : [...ids, id]
}

/**
 * Create/edit form for a MenuItem — name, category, description/image, base price, base
 * ingredients + allowed extras (checkbox multi-select from the ingredient dictionary) and a
 * variants editor (ADR-0016, see MenuItemVariantsEditor). Shared between "Add new item" and
 * "Edit" on AdminMenuPage.
 */
export function MenuItemForm({ mode, item, ingredients, onSaved, onCancel }: MenuItemFormProps) {
  const [name, setName] = useState(item?.name ?? '')
  const [category, setCategory] = useState<MenuCategory>(item?.category ?? 'Pizza')
  const [description, setDescription] = useState(item?.description ?? '')
  const [imageUrl, setImageUrl] = useState(item?.imageUrl ?? '')
  const [basePriceAmount, setBasePriceAmount] = useState(item?.basePrice.amount ?? 0)
  const [currency] = useState(item?.basePrice.currency ?? 'PLN')
  const [baseIngredientIds, setBaseIngredientIds] = useState<string[]>(item?.baseIngredients.map((i) => i.id) ?? [])
  const [allowedExtraIds, setAllowedExtraIds] = useState<string[]>(item?.allowedExtras.map((i) => i.id) ?? [])
  const [variants, setVariants] = useState<MenuItemVariantInput[]>(
    item?.variants.map((v) => ({ id: v.id, name: v.name, price: v.price, isDefault: v.isDefault })) ?? [],
  )

  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]> | null>(null)

  const [uploadingImage, setUploadingImage] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)

  /**
   * Runs immediately on file selection, independent of the main form submit — upload requires
   * an existing MenuItem.Id, so it's only wired up in edit mode (see JSX below).
   */
  async function handleImageSelected(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file || !item) return

    setUploadingImage(true)
    setUploadError(null)
    try {
      const newImageUrl = await uploadMenuItemImage(item.id, file)
      setImageUrl(newImageUrl)
    } catch (err) {
      setUploadError(err instanceof ApiError ? err.detail ?? err.title ?? err.message : 'Nie udało się wgrać zdjęcia.')
    } finally {
      setUploadingImage(false)
    }
  }

  async function handleRemoveImage() {
    if (!item) return

    setUploadingImage(true)
    setUploadError(null)
    try {
      await updateMenuItem(item.id, {
        name,
        description: description || null,
        imageUrl: null,
        basePrice: { amount: basePriceAmount, currency },
        baseIngredientIds,
        allowedExtraIds,
        variants,
      })
      setImageUrl('')
    } catch (err) {
      setUploadError(err instanceof ApiError ? err.detail ?? err.title ?? err.message : 'Nie udało się usunąć zdjęcia.')
    } finally {
      setUploadingImage(false)
    }
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setSubmitError(null)
    setFieldErrors(null)

    const basePrice = { amount: basePriceAmount, currency }

    try {
      if (mode === 'create') {
        // No image upload in create mode (it needs an existing MenuItem.Id) — imageUrl is
        // always null here, added afterwards in edit mode.
        await createMenuItem({
          name,
          category,
          basePrice,
          description: description || null,
          imageUrl: null,
          baseIngredientIds,
          allowedExtraIds,
          variants,
        })
      } else if (item) {
        const command: Omit<UpdateMenuItemCommand, 'id'> = {
          name,
          description: description || null,
          // Preserve the current image — this form no longer has a text input for it, it's
          // only changed via uploadMenuItemImage/handleRemoveImage (immediate, not part of
          // this submit). Sending anything else here would silently clear it (PUT/replace
          // semantics, see UpdateMenuItemCommand doc comment).
          imageUrl: imageUrl || null,
          basePrice,
          baseIngredientIds,
          allowedExtraIds,
          variants,
        }
        await updateMenuItem(item.id, command)
      }
      onSaved()
    } catch (err) {
      if (err instanceof ApiError && err.status === 400 && err.errors) {
        setFieldErrors(err.errors)
        setSubmitError(err.detail ?? err.title ?? 'Popraw błędy w formularzu.')
      } else if (err instanceof ApiError) {
        setSubmitError(err.detail ?? err.title ?? err.message)
      } else {
        setSubmitError('Nie udało się zapisać pozycji menu.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="checkout-step admin-form" onSubmit={handleSubmit}>
      <h4>{mode === 'create' ? 'Nowa pozycja menu' : `Edytuj: ${item?.name}`}</h4>

      <div className="checkout-form-grid">
        <label className="checkout-field">
          Nazwa
          <input value={name} onChange={(e) => setName(e.target.value)} required />
        </label>

        <label className="checkout-field">
          Kategoria
          <select value={category} onChange={(e) => setCategory(e.target.value as MenuCategory)}>
            {MENU_CATEGORIES.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </label>

        <label className="checkout-field">
          Opis (opcjonalnie)
          <input value={description} onChange={(e) => setDescription(e.target.value)} />
        </label>

        <label className="checkout-field">
          Cena bazowa
          <input
            type="number"
            step="0.01"
            min="0"
            value={basePriceAmount}
            onChange={(e) => setBasePriceAmount(Number(e.target.value))}
          />
        </label>
      </div>

      <fieldset className="admin-fieldset">
        <legend>Zdjęcie</legend>
        {mode === 'create' ? (
          <p>Zdjęcie będzie można dodać po zapisaniu, w trybie edycji.</p>
        ) : (
          <>
            {imageUrl && <img src={imageUrl} alt={item?.name} className="admin-menu-item-image-preview" />}
            <div className="checkout-actions">
              <label className="add-to-cart-btn admin-file-input-label">
                {uploadingImage ? 'Wgrywanie...' : 'Wgraj zdjęcie'}
                <input
                  type="file"
                  accept="image/jpeg,image/png,image/webp"
                  onChange={handleImageSelected}
                  disabled={uploadingImage}
                  hidden
                />
              </label>
              {imageUrl && (
                <button type="button" onClick={handleRemoveImage} disabled={uploadingImage}>
                  Usuń zdjęcie
                </button>
              )}
            </div>
            {uploadError && <p className="checkout-error">{uploadError}</p>}
          </>
        )}
      </fieldset>

      <fieldset className="admin-fieldset">
        <legend>Składniki bazowe</legend>
        <div className="admin-checkbox-list">
          {ingredients.map((ingredient) => (
            <label key={ingredient.id} className="admin-checkbox-list__item">
              <input
                type="checkbox"
                checked={baseIngredientIds.includes(ingredient.id)}
                onChange={() => setBaseIngredientIds((ids) => toggleId(ids, ingredient.id))}
              />
              {ingredient.name}
            </label>
          ))}
        </div>
      </fieldset>

      <fieldset className="admin-fieldset">
        <legend>Dozwolone dodatki</legend>
        <div className="admin-checkbox-list">
          {ingredients.map((ingredient) => (
            <label key={ingredient.id} className="admin-checkbox-list__item">
              <input
                type="checkbox"
                checked={allowedExtraIds.includes(ingredient.id)}
                onChange={() => setAllowedExtraIds((ids) => toggleId(ids, ingredient.id))}
              />
              {ingredient.name} (+{ingredient.extraPrice.amount.toFixed(2)} {ingredient.extraPrice.currency})
            </label>
          ))}
        </div>
      </fieldset>

      <fieldset className="admin-fieldset">
        <legend>Warianty</legend>
        <MenuItemVariantsEditor variants={variants} currency={currency} onChange={setVariants} />
      </fieldset>

      {fieldErrors && (
        <ul className="checkout-error-list">
          {Object.entries(fieldErrors).map(([field, messages]) => (
            <li key={field}>
              {field}: {messages.join(' ')}
            </li>
          ))}
        </ul>
      )}
      {submitError && <p className="checkout-error">{submitError}</p>}

      <div className="checkout-actions">
        <button type="button" onClick={onCancel} disabled={submitting}>
          Anuluj
        </button>
        <button type="submit" className="add-to-cart-btn" disabled={submitting}>
          {submitting ? 'Zapisywanie...' : 'Zapisz'}
        </button>
      </div>
    </form>
  )
}
