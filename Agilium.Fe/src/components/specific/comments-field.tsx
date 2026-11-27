import * as React from "react";
import Markdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { MAX_COMMENT_LENGTH } from "../../services/comments-api";
import { useAddComment, useComments, useDeleteComment, useEditComment } from "../../services/comments-queries";
import { ApiError } from "../../services/http-client";

function errorText(error: unknown): string {
  return error instanceof ApiError ? error.message : "Operace se nezdařila.";
}

/** Markdown without raw HTML (react-markdown does not render it), links open in a new tab. */
export function MarkdownText({ text }: { text: string }) {
  return (
    <div className="prose prose-sm max-w-none break-words [&_a]:text-blue-600 [&_a]:underline [&_code]:rounded [&_code]:bg-gray-100 [&_code]:px-1 [&_ol]:list-decimal [&_ol]:pl-5 [&_pre]:overflow-x-auto [&_pre]:rounded [&_pre]:bg-gray-100 [&_pre]:p-2 [&_ul]:list-disc [&_ul]:pl-5">
      <Markdown
        remarkPlugins={[remarkGfm]}
        components={{ a: (props) => <a {...props} target="_blank" rel="noopener noreferrer nofollow" /> }}
      >
        {text}
      </Markdown>
    </div>
  );
}

/** A textarea with a preview switch, used for both a new comment and the edit of an existing one. */
function MarkdownEditor({
  label,
  initial,
  submitLabel,
  pending,
  error,
  onSubmit,
  onCancel,
}: {
  label: string;
  initial: string;
  submitLabel: string;
  pending: boolean;
  error: unknown;
  onSubmit: (text: string) => Promise<void>;
  onCancel?: () => void;
}) {
  const [text, setText] = React.useState(initial);
  const [preview, setPreview] = React.useState(false);
  const blank = text.trim() === "";

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (blank) return;
    try {
      await onSubmit(text);
      setText("");
      setPreview(false);
    } catch {
      // shown from the error prop
    }
  }

  return (
    <form onSubmit={handleSubmit} className="mt-2">
      {preview ? (
        <div aria-label={`${label} (náhled)`} className="min-h-[5rem] rounded border border-gray-200 bg-gray-50 p-2">
          {blank ? <p className="text-sm text-gray-400">Nic k zobrazení.</p> : <MarkdownText text={text} />}
        </div>
      ) : (
        <textarea
          aria-label={label}
          rows={4}
          maxLength={MAX_COMMENT_LENGTH}
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder="Markdown: **tučně**, *kurzíva*, `kód`, seznamy, odkazy…"
          className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
        />
      )}
      {error != null && (
        <p role="alert" className="mt-1 text-sm text-red-600">
          {errorText(error)}
        </p>
      )}
      <div className="mt-2 flex items-center gap-3 text-sm">
        <button
          type="submit"
          disabled={pending || blank}
          className="rounded bg-blue-600 px-3 py-1 font-medium text-white hover:bg-blue-700 disabled:opacity-50"
        >
          {submitLabel}
        </button>
        <button type="button" onClick={() => setPreview((p) => !p)} className="text-blue-600 hover:underline">
          {preview ? "Upravit" : "Náhled"}
        </button>
        {onCancel && (
          <button type="button" onClick={onCancel} className="text-gray-500 hover:underline">
            Zrušit
          </button>
        )}
        <span className="ml-auto text-xs text-gray-400">
          {text.length} / {MAX_COMMENT_LENGTH}
        </span>
      </div>
    </form>
  );
}

function formatTime(iso: string): string {
  return new Date(iso.endsWith("Z") ? iso : `${iso}Z`).toLocaleString("cs-CZ");
}

/** The thread of one Comments element of an item: list, add, edit (own), delete (own, or by a project manager). */
export function CommentsField({
  itemId,
  templateItemId,
  title,
  canWrite,
}: {
  itemId: number;
  templateItemId: number;
  title: string;
  /** Whether the caller may add comments (project permission EditItems; the server enforces it). */
  canWrite: boolean;
}) {
  const comments = useComments(itemId, templateItemId);
  const add = useAddComment(itemId, templateItemId);
  const edit = useEditComment(itemId, templateItemId);
  const remove = useDeleteComment(itemId, templateItemId);
  const [editingId, setEditingId] = React.useState<number | null>(null);

  return (
    <div>
      <h3 className="mb-1 block text-sm font-medium text-gray-700">{title}</h3>
      {comments.isLoading && <p className="text-sm text-gray-500">Načítání komentářů…</p>}
      {comments.error && <p className="text-sm text-red-600">{errorText(comments.error)}</p>}
      {comments.data?.length === 0 && <p className="text-sm text-gray-500">Zatím žádné komentáře.</p>}

      <ul className="space-y-3">
        {comments.data?.map((c) => (
          <li key={c.id} className="rounded border border-gray-200 p-3">
            <div className="flex flex-wrap items-center gap-2 text-xs text-gray-500">
              <strong className="text-gray-700">{c.authorName}</strong>
              <span>{formatTime(c.createdAt)}</span>
              {c.editedAt && <span>(upraveno {formatTime(c.editedAt)})</span>}
              <span className="mr-auto" />
              {c.canEdit && editingId !== c.id && (
                <button
                  type="button"
                  onClick={() => {
                    edit.reset();
                    setEditingId(c.id);
                  }}
                  className="text-blue-600 hover:underline"
                >
                  Upravit
                </button>
              )}
              {c.canDelete && (
                <button
                  type="button"
                  disabled={remove.isPending}
                  onClick={() => {
                    if (window.confirm("Smazat komentář?")) remove.mutate(c.id);
                  }}
                  className="text-red-600 hover:underline"
                >
                  Smazat
                </button>
              )}
            </div>
            {editingId === c.id ? (
              <MarkdownEditor
                label="Upravit komentář"
                initial={c.text}
                submitLabel="Uložit změnu"
                pending={edit.isPending}
                error={edit.error}
                onCancel={() => setEditingId(null)}
                onSubmit={async (text) => {
                  await edit.mutateAsync({ id: c.id, text });
                  setEditingId(null);
                }}
              />
            ) : (
              <div className="mt-1">
                <MarkdownText text={c.text} />
              </div>
            )}
          </li>
        ))}
      </ul>
      {remove.error && (
        <p role="alert" className="mt-1 text-sm text-red-600">
          {errorText(remove.error)}
        </p>
      )}

      {canWrite && (
        <MarkdownEditor
          label="Nový komentář"
          initial=""
          submitLabel="Přidat komentář"
          pending={add.isPending}
          error={add.error}
          onSubmit={async (text) => {
            await add.mutateAsync(text);
          }}
        />
      )}
    </div>
  );
}
